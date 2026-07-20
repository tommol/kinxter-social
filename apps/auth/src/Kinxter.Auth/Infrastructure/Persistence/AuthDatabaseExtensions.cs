using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth.Infrastructure.Persistence;

internal static class AuthDatabaseExtensions
{
    public static async Task ApplyAuthDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var options = services.GetRequiredService<AuthServerOptions>();
        var dbContext = services.GetRequiredService<AuthDbContext>();

        await CreateSchemaAsync(dbContext, options.DbSchema);
        await dbContext.Database.MigrateAsync();
        await SeedAuthRealmsAsync(services, options);
        await SeedOpenIddictAsync(services, options);

        foreach (var realmOptions in options.Realms)
        {
            await SeedRealmAdminAsync(services, realmOptions);
        }
    }

    private static async Task CreateSchemaAsync(AuthDbContext dbContext, string schema)
    {
        var escapedSchema = schema.Replace("\"", "\"\"", StringComparison.Ordinal);
        var sql = "CREATE SCHEMA IF NOT EXISTS \"" + escapedSchema + "\";";

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedOpenIddictAsync(IServiceProvider services, AuthServerOptions options)
    {
        var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();

        await CreateScopeAsync(scopeManager, AuthScopes.KinxterApi, "Kinxter public API", "kinxter-api");
        await CreateScopeAsync(scopeManager, AuthScopes.KinxterAdmin, "Kinxter backoffice API", "kinxter-api");

        foreach (var client in options.Realms.SelectMany(realm => realm.Clients))
        {
            var descriptor = CreateOpenIddictApplicationDescriptor(client);
            var application = await applicationManager.FindByClientIdAsync(client.ClientId);

            if (application is null)
            {
                await applicationManager.CreateAsync(descriptor);
            }
            else
            {
                await applicationManager.UpdateAsync(application, descriptor);
            }
        }
    }

    private static OpenIddictApplicationDescriptor CreateOpenIddictApplicationDescriptor(AuthClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = client.DisplayName,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.PushedAuthorization,
                Permissions.Endpoints.Revocation,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var redirectUri in client.RedirectUris.Where(uri => !string.IsNullOrWhiteSpace(uri)))
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        foreach (var logoutUri in client.PostLogoutRedirectUris.Where(uri => !string.IsNullOrWhiteSpace(uri)))
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(logoutUri));
        }

        descriptor.AddScopePermissions(client.Scopes
            .Where(scope => !IsStandardScope(scope))
            .ToArray());

        return descriptor;
    }

    private static async Task SeedAuthRealmsAsync(IServiceProvider services, AuthServerOptions options)
    {
        var dbContext = services.GetRequiredService<AuthDbContext>();
        var clock = services.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        foreach (var realmOptions in options.Realms)
        {
            var realm = await dbContext.AuthRealms
                .Include(current => current.Clients)
                .SingleOrDefaultAsync(current => current.Name == realmOptions.Realm);

            if (realm is null)
            {
                realm = new AuthRealm
                {
                    Id = Guid.CreateVersion7(now),
                    Name = realmOptions.Realm,
                    CreatedAt = now
                };

                dbContext.AuthRealms.Add(realm);
            }
            else
            {
                realm.UpdatedAt = now;
            }

            realm.Issuer = realmOptions.Issuer;
            realm.PathBase = realmOptions.PathBase;
            realm.MfaPolicy = realmOptions.MfaPolicy;
            realm.SignupEnabled = realmOptions.SignupEnabled;

            UpsertAuthClients(realm, realmOptions.Clients, now);
        }

        await dbContext.SaveChangesAsync();
    }

    private static void UpsertAuthClients(
        AuthRealm realm,
        IReadOnlyCollection<AuthClientOptions> clientOptions,
        DateTimeOffset now)
    {
        var configuredClientIds = clientOptions
            .Select(client => client.ClientId)
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId))
            .Select(clientId => clientId.Trim())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var configuredClient in clientOptions.Where(client => !string.IsNullOrWhiteSpace(client.ClientId)))
        {
            var clientId = configuredClient.ClientId.Trim();
            var client = realm.Clients.SingleOrDefault(current =>
                string.Equals(current.ClientId, clientId, StringComparison.Ordinal));

            if (client is null)
            {
                client = new AuthClient
                {
                    Id = Guid.CreateVersion7(now),
                    RealmId = realm.Id,
                    ClientId = clientId,
                    CreatedAt = now
                };

                realm.Clients.Add(client);
            }
            else
            {
                client.UpdatedAt = now;
            }

            client.DisplayName = configuredClient.DisplayName;
            client.Enabled = true;
            client.ClientSecretConfigured = !string.IsNullOrWhiteSpace(configuredClient.ClientSecret);
            client.RedirectUris = CleanValues(configuredClient.RedirectUris);
            client.PostLogoutRedirectUris = CleanValues(configuredClient.PostLogoutRedirectUris);
            client.Scopes = CleanValues(configuredClient.Scopes);
        }

        foreach (var removedClient in realm.Clients.Where(client => !configuredClientIds.Contains(client.ClientId)))
        {
            removedClient.Enabled = false;
            removedClient.UpdatedAt = now;
        }
    }

    private static string[] CleanValues(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task CreateScopeAsync(
        IOpenIddictScopeManager scopeManager,
        string name,
        string displayName,
        string resource)
    {
        if (await scopeManager.FindByNameAsync(name) is not null)
        {
            return;
        }

        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = displayName,
            Resources =
            {
                resource
            }
        });
    }

    private static async Task SeedRealmAdminAsync(IServiceProvider services, AuthOptions options)
    {
        if (!options.SeedAdmin.Enabled ||
            string.IsNullOrWhiteSpace(options.SeedAdmin.Email) ||
            string.IsNullOrWhiteSpace(options.SeedAdmin.Password))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<AuthUser>>();
        var dbContext = services.GetRequiredService<AuthDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var clock = services.GetRequiredService<IClock>();
        var email = options.SeedAdmin.Email.Trim();

        if (!await roleManager.RoleExistsAsync(AuthRoles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(AuthRoles.Admin));
        }

        var user = await userManager.FindByEmailInRealmAsync(dbContext, options, email);

        if (user is null)
        {
            user = new AuthUser
            {
                Id = Guid.CreateVersion7(clock.UtcNow),
                Realm = options.Realm,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = clock.UtcNow
            };

            var result = await userManager.CreateAsync(user, options.SeedAdmin.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Admin seed for realm '{options.Realm}' failed: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, AuthRoles.Admin))
        {
            await userManager.AddToRoleAsync(user, AuthRoles.Admin);
        }
    }

    private static bool IsStandardScope(string scope)
    {
        return string.Equals(scope, Scopes.OpenId, StringComparison.Ordinal) ||
            string.Equals(scope, Scopes.Profile, StringComparison.Ordinal) ||
            string.Equals(scope, Scopes.Email, StringComparison.Ordinal) ||
            string.Equals(scope, Scopes.Roles, StringComparison.Ordinal);
    }
}

internal static class AuthRoles
{
    public const string Admin = "admin";
}
