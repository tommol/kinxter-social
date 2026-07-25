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
        await SeedAuthAdministratorAsync(services);

        foreach (var realmOptions in options.Realms)
        {
            await SeedRealmAdminAsync(services, realmOptions);
        }

        await LoadRealmRegistryAsync(services);
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
            var application = await applicationManager.FindByClientIdAsync(client.ClientId);

            if (application is null)
            {
                await applicationManager.CreateAsync(
                    OpenIddictClientDescriptorFactory.Create(client));
            }
        }
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
                    Issuer = realmOptions.Issuer,
                    PathBase = realmOptions.PathBase,
                    MfaPolicy = realmOptions.MfaPolicy,
                    SignupEnabled = realmOptions.SignupEnabled,
                    CreatedAt = now
                };

                dbContext.AuthRealms.Add(realm);
            }

            BootstrapAuthClients(realm, realmOptions.Clients, now);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAuthAdministratorAsync(IServiceProvider services)
    {
        var options = services.GetRequiredService<AuthAdminOptions>();

        if (!options.Enabled || !options.Bootstrap.IsConfigured)
        {
            return;
        }

        var dbContext = services.GetRequiredService<AuthDbContext>();
        var normalizer = services.GetRequiredService<ILookupNormalizer>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher<AuthAdministrator>>();
        var clock = services.GetRequiredService<IClock>();
        var username = options.Bootstrap.Username!.Trim();
        var normalizedUsername = normalizer.NormalizeName(username)
            ?? throw new InvalidOperationException("The auth administrator username could not be normalized.");
        var exists = await dbContext.AuthAdministrators
            .AnyAsync(administrator => administrator.NormalizedUsername == normalizedUsername);

        if (exists)
        {
            return;
        }

        var administrator = new AuthAdministrator
        {
            Id = Guid.CreateVersion7(clock.UtcNow),
            Username = username,
            NormalizedUsername = normalizedUsername,
            CreatedAt = clock.UtcNow
        };
        administrator.PasswordHash = passwordHasher.HashPassword(
            administrator,
            options.Bootstrap.Password!);

        dbContext.AuthAdministrators.Add(administrator);
        await dbContext.SaveChangesAsync();
    }

    private static async Task LoadRealmRegistryAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<AuthDbContext>();
        var realmRegistry = services.GetRequiredService<AuthRealmRegistry>();
        var realms = await dbContext.AuthRealms
            .AsNoTracking()
            .OrderBy(realm => realm.Name)
            .ToArrayAsync();

        realmRegistry.Replace(realms);
    }

    private static void BootstrapAuthClients(
        AuthRealm realm,
        IReadOnlyCollection<AuthClientOptions> clientOptions,
        DateTimeOffset now)
    {
        foreach (var configuredClient in clientOptions.Where(client => !string.IsNullOrWhiteSpace(client.ClientId)))
        {
            var clientId = configuredClient.ClientId.Trim();
            var client = realm.Clients.SingleOrDefault(current =>
                string.Equals(current.ClientId, clientId, StringComparison.Ordinal));

            if (client is not null)
            {
                continue;
            }

            realm.Clients.Add(new AuthClient
            {
                Id = Guid.CreateVersion7(now),
                RealmId = realm.Id,
                ClientId = clientId,
                DisplayName = configuredClient.DisplayName,
                Enabled = true,
                ClientSecretConfigured = !string.IsNullOrWhiteSpace(configuredClient.ClientSecret),
                RedirectUris = CleanValues(configuredClient.RedirectUris),
                PostLogoutRedirectUris = CleanValues(configuredClient.PostLogoutRedirectUris),
                Scopes = CleanValues(configuredClient.Scopes),
                CreatedAt = now
            });
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
