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
        var options = services.GetRequiredService<AuthOptions>();
        var dbContext = services.GetRequiredService<AuthDbContext>();

        await CreateSchemaAsync(dbContext, options.DbSchema);
        await dbContext.Database.MigrateAsync();
        await SeedOpenIddictAsync(services, options);
        await SeedBackofficeAdminAsync(services, options);
    }

    private static async Task CreateSchemaAsync(AuthDbContext dbContext, string schema)
    {
        var escapedSchema = schema.Replace("\"", "\"\"", StringComparison.Ordinal);
        var sql = "CREATE SCHEMA IF NOT EXISTS \"" + escapedSchema + "\";";

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedOpenIddictAsync(IServiceProvider services, AuthOptions options)
    {
        var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();

        await CreateScopeAsync(scopeManager, AuthScopes.KinxterApi, "Kinxter public API", "kinxter-api");
        await CreateScopeAsync(scopeManager, AuthScopes.KinxterAdmin, "Kinxter backoffice API", "kinxter-api");

        foreach (var client in options.Clients)
        {
            if (await applicationManager.FindByClientIdAsync(client.ClientId) is not null)
            {
                continue;
            }

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

            await applicationManager.CreateAsync(descriptor);
        }
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

    private static async Task SeedBackofficeAdminAsync(IServiceProvider services, AuthOptions options)
    {
        if (options.Realm != AuthRealms.Backoffice ||
            !options.SeedAdmin.Enabled ||
            string.IsNullOrWhiteSpace(options.SeedAdmin.Email) ||
            string.IsNullOrWhiteSpace(options.SeedAdmin.Password))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<AuthUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var email = options.SeedAdmin.Email.Trim();

        if (!await roleManager.RoleExistsAsync(AuthRoles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(AuthRoles.Admin));
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new AuthUser
            {
                Id = Guid.CreateVersion7(),
                Realm = options.Realm,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(user, options.SeedAdmin.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Backoffice admin seed failed: {errors}");
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
