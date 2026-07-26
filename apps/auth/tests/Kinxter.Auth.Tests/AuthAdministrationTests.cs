using Kinxter.Auth.Administration;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Xunit;

namespace Kinxter.Auth.Tests;

public sealed class AuthAdministrationTests
{
    [Fact]
    public void Backoffice_roles_resolve_to_least_privilege_permissions()
    {
        var operations = AuthRoles.GetPermissions([AuthRoles.Operations]);
        var support = AuthRoles.GetPermissions([AuthRoles.Support]);
        var superAdmin = AuthRoles.GetPermissions([AuthRoles.SuperAdmin]);
        var legacyAdmin = AuthRoles.GetPermissions([AuthRoles.LegacyAdmin]);

        Assert.Contains(AuthPermissions.AdminAccess, operations);
        Assert.Contains(AuthPermissions.MonitoringRead, operations);
        Assert.DoesNotContain(AuthPermissions.AdminUsersManage, operations);
        Assert.Contains(AuthPermissions.UsersManage, support);
        Assert.DoesNotContain(AuthPermissions.MonitoringRead, support);
        Assert.Equal(AuthPermissions.All.Order(), superAdmin.Order());
        Assert.Equal(AuthPermissions.All.Order(), legacyAdmin.Order());
    }

    [Fact]
    public void AuthAdminOptions_reads_independent_control_plane_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthAdmin:Enabled"] = "true",
                ["AuthAdmin:PathBase"] = "/operations/auth/",
                ["AuthAdmin:CookieName"] = "auth-operations",
                ["AuthAdmin:SessionHours"] = "12",
                ["AuthAdmin:Bootstrap:Username"] = "root-admin",
                ["AuthAdmin:Bootstrap:Password"] = "secure-secret-value"
            })
            .Build();

        var options = AuthAdminOptions.FromConfiguration(configuration);

        Assert.True(options.Enabled);
        Assert.Equal("/operations/auth", options.PathBase);
        Assert.Equal("/operations/auth/login", options.LoginPath);
        Assert.Equal("auth-operations", options.CookieName);
        Assert.Equal(12, options.SessionHours);
        Assert.True(options.Bootstrap.IsConfigured);
        Assert.Equal("root-admin", options.Bootstrap.Username);
    }

    [Theory]
    [InlineData("/realms/control")]
    [InlineData("/account/control")]
    [InlineData("/control/../realm")]
    public void AuthAdminOptions_rejects_a_reserved_or_unsafe_path(string pathBase)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthAdmin:PathBase"] = pathBase
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => AuthAdminOptions.FromConfiguration(configuration));

        Assert.Contains("safe, non-realm", error.Message);
    }

    [Fact]
    public void AuthAdminOptions_rejects_a_weak_bootstrap_password()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthAdmin:Bootstrap:Username"] = "admin",
                ["AuthAdmin:Bootstrap:Password"] = "too-short"
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => AuthAdminOptions.FromConfiguration(configuration));

        Assert.Contains("at least 12 characters", error.Message);
    }

    [Fact]
    public void Persisted_realm_settings_override_bootstrap_without_losing_static_integrations()
    {
        var configuredClient = new AuthClientOptions
        {
            ClientId = "kinxter-web",
            ClientSecret = "secret"
        };
        var bootstrap = new AuthServerOptions
        {
            DbSchema = "auth",
            CookieName = "realm-cookie",
            EncryptionKey = "encryption-key",
            Realms =
            [
                new AuthOptions
                {
                    Realm = "public",
                    Issuer = "https://old.example/realms/public",
                    PathBase = "/realms/public",
                    AllowedOrigins = ["https://web.example"],
                    Clients = [configuredClient]
                }
            ]
        };
        var registry = new AuthRealmRegistry(bootstrap);

        registry.Replace(
        [
            new AuthRealm
            {
                Id = Guid.CreateVersion7(),
                Name = "public",
                Issuer = "https://auth.example/realms/public-v2",
                PathBase = "/realms/public-v2",
                MfaPolicy = AuthMfaPolicy.Required,
                SignupEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow
            }
        ]);

        var realm = Assert.Single(registry.Realms);
        Assert.Equal("https://auth.example/realms/public-v2", realm.Issuer);
        Assert.Equal("/realms/public-v2", realm.PathBase);
        Assert.True(realm.RequiresMfa);
        Assert.False(realm.SignupEnabled);
        Assert.Equal(["https://web.example"], realm.AllowedOrigins);
        Assert.Same(configuredClient, Assert.Single(realm.Clients));
    }

    [Fact]
    public async Task UpdateRealm_persists_settings_and_updates_runtime_registry()
    {
        await using var dbContext = CreateDbContext();
        var persistedRealm = CreateRealm();
        dbContext.AuthRealms.Add(persistedRealm);
        await dbContext.SaveChangesAsync();
        var registry = CreateRegistry();
        registry.Replace([persistedRealm]);
        var now = new DateTimeOffset(2026, 7, 25, 10, 30, 0, TimeSpan.Zero);
        var service = new AuthAdministrationService(
            dbContext,
            registry,
            new TestClock(now));

        var result = await service.UpdateRealmAsync(
            persistedRealm.Id,
            new AuthAdminUpdateRealmCommand(
                "https://identity.example/realms/customer",
                "/realms/customer",
                AuthMfaPolicy.Required,
                false));

        Assert.True(result.Success);
        var storedRealm = await dbContext.AuthRealms.SingleAsync();
        Assert.Equal("https://identity.example/realms/customer", storedRealm.Issuer);
        Assert.Equal("/realms/customer", storedRealm.PathBase);
        Assert.Equal(AuthMfaPolicy.Required, storedRealm.MfaPolicy);
        Assert.False(storedRealm.SignupEnabled);
        Assert.Equal(now, storedRealm.UpdatedAt);

        var runtimeRealm = Assert.Single(registry.Realms);
        Assert.Equal("/realms/customer", runtimeRealm.PathBase);
        Assert.True(runtimeRealm.RequiresMfa);
        Assert.False(runtimeRealm.SignupEnabled);
    }

    [Fact]
    public async Task UpdateRealm_rejects_an_issuer_whose_path_does_not_match()
    {
        await using var dbContext = CreateDbContext();
        var persistedRealm = CreateRealm();
        dbContext.AuthRealms.Add(persistedRealm);
        await dbContext.SaveChangesAsync();
        var registry = CreateRegistry();
        registry.Replace([persistedRealm]);
        var service = new AuthAdministrationService(
            dbContext,
            registry,
            new TestClock(DateTimeOffset.UtcNow));

        var result = await service.UpdateRealmAsync(
            persistedRealm.Id,
            new AuthAdminUpdateRealmCommand(
                "https://identity.example/realms/another",
                "/realms/public",
                AuthMfaPolicy.Required,
                false));

        Assert.False(result.Success);
        Assert.Contains("must match", result.Error);
        Assert.Equal("/realms/public", (await dbContext.AuthRealms.SingleAsync()).PathBase);
        Assert.Equal("https://auth.example/realms/public", Assert.Single(registry.Realms).Issuer);
    }

    [Fact]
    public async Task UpdateRealm_rejects_a_case_insensitive_path_collision()
    {
        await using var dbContext = CreateDbContext();
        var persistedRealm = CreateRealm();
        dbContext.AuthRealms.AddRange(
            persistedRealm,
            new AuthRealm
            {
                Id = Guid.CreateVersion7(),
                Name = "backoffice",
                Issuer = "https://auth.example/realms/backoffice",
                PathBase = "/realms/backoffice",
                MfaPolicy = AuthMfaPolicy.Required,
                SignupEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
        await dbContext.SaveChangesAsync();
        var registry = CreateRegistry();
        registry.Replace([persistedRealm]);
        var service = new AuthAdministrationService(
            dbContext,
            registry,
            new TestClock(DateTimeOffset.UtcNow));

        var result = await service.UpdateRealmAsync(
            persistedRealm.Id,
            new AuthAdminUpdateRealmCommand(
                "https://identity.example/REALMS/BACKOFFICE",
                "/REALMS/BACKOFFICE",
                AuthMfaPolicy.OptionalStepUp,
                true));

        Assert.False(result.Success);
        Assert.Contains("already used", result.Error);
    }

    [Fact]
    public async Task Client_management_persists_registration_and_preserves_secret_during_edit()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString("D");
        var now = new DateTimeOffset(2026, 7, 26, 9, 0, 0, TimeSpan.Zero);
        services.AddLogging();
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
            options.UseOpenIddict();
        });
        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>());
        services.AddSingleton<IClock>(new TestClock(now));
        services.AddScoped<AuthClientAdministrationService>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var realm = CreateRealm();
        dbContext.AuthRealms.Add(realm);
        await dbContext.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<AuthClientAdministrationService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var created = await service.CreateClientAsync(
            realm.Id,
            new AuthAdminCreateClientCommand(
                "standard-web",
                "Standard web",
                AuthClientType.Confidential,
                [AuthClientGrantTypes.AuthorizationCode, AuthClientGrantTypes.RefreshToken],
                ["https://web.example/auth/callback"],
                ["https://web.example"],
                ["openid", "profile", "offline_access", "kinxter.api"]));

        Assert.True(created.Success);
        Assert.NotNull(created.ClientSecret);
        var storedClient = await dbContext.AuthClients.SingleAsync();
        Assert.Equal(realm.Id, storedClient.RealmId);
        var application = await manager.FindByClientIdAsync("standard-web");
        Assert.NotNull(application);
        Assert.True(await manager.ValidateClientSecretAsync(application!, created.ClientSecret!));

        var updated = await service.UpdateClientAsync(
            realm.Id,
            storedClient.Id,
            new AuthAdminUpdateClientCommand(
                "Renamed display",
                false,
                AuthClientType.Confidential,
                [AuthClientGrantTypes.AuthorizationCode, AuthClientGrantTypes.RefreshToken],
                ["https://web.example/api/auth/callback/kinxter"],
                ["https://web.example/signed-out"],
                ["openid", "email", "kinxter.api"]));

        Assert.True(updated.Success);
        Assert.False((await dbContext.AuthClients.SingleAsync()).Enabled);
        application = await manager.FindByClientIdAsync("standard-web");
        Assert.NotNull(application);
        Assert.True(await manager.ValidateClientSecretAsync(application!, created.ClientSecret!));
        Assert.Equal(
            new[] { "https://web.example/api/auth/callback/kinxter" },
            (await manager.GetRedirectUrisAsync(application!)).ToArray());

        var rotated = await service.RotateSecretAsync(realm.Id, storedClient.Id);

        Assert.True(rotated.Success);
        Assert.NotNull(rotated.ClientSecret);
        application = await manager.FindByClientIdAsync("standard-web");
        Assert.NotNull(application);
        Assert.False(await manager.ValidateClientSecretAsync(application!, created.ClientSecret!));
        Assert.True(await manager.ValidateClientSecretAsync(application!, rotated.ClientSecret!));
        Assert.Equal(now, (await dbContext.AuthClients.SingleAsync()).UpdatedAt);
    }

    [Fact]
    public async Task Client_management_accepts_oauth_only_and_rejects_unsafe_or_incompatible_registrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("D"));
            options.UseOpenIddict();
        });
        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>());
        services.AddSingleton<IClock>(new TestClock(DateTimeOffset.UtcNow));
        services.AddScoped<AuthClientAdministrationService>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var realm = CreateRealm();
        dbContext.AuthRealms.Add(realm);
        await dbContext.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<AuthClientAdministrationService>();

        var oauthOnly = await service.CreateClientAsync(
            realm.Id,
            new AuthAdminCreateClientCommand(
                "service-client",
                "Service client",
                AuthClientType.Confidential,
                [AuthClientGrantTypes.ClientCredentials],
                [],
                [],
                ["kinxter.api"]));
        var publicMachineClient = await service.CreateClientAsync(
            realm.Id,
            new AuthAdminCreateClientCommand(
                "public-machine",
                "Public machine client",
                AuthClientType.Public,
                [AuthClientGrantTypes.ClientCredentials],
                [],
                [],
                ["kinxter.api"]));
        var unsafeRedirect = await service.CreateClientAsync(
            realm.Id,
            new AuthAdminCreateClientCommand(
                "unsafe-client",
                "Unsafe client",
                AuthClientType.Public,
                [AuthClientGrantTypes.AuthorizationCode],
                ["javascript:alert(1)"],
                [],
                ["openid"]));

        Assert.True(oauthOnly.Success);
        Assert.NotNull(oauthOnly.ClientSecret);
        Assert.False(publicMachineClient.Success);
        Assert.Contains("confidential", publicMachineClient.Error);
        Assert.False(unsafeRedirect.Success);
        Assert.Contains("HTTP or HTTPS", unsafeRedirect.Error);
        Assert.Single(await dbContext.AuthClients.ToArrayAsync());
    }

    [Fact]
    public async Task Public_device_client_has_no_secret_and_type_change_generates_one()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("D"));
            options.UseOpenIddict();
        });
        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>());
        services.AddSingleton<IClock>(new TestClock(DateTimeOffset.UtcNow));
        services.AddScoped<AuthClientAdministrationService>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var realm = CreateRealm();
        dbContext.AuthRealms.Add(realm);
        await dbContext.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<AuthClientAdministrationService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var created = await service.CreateClientAsync(
            realm.Id,
            new AuthAdminCreateClientCommand(
                "living-room-device",
                "Living room device",
                AuthClientType.Public,
                [AuthClientGrantTypes.DeviceCode, AuthClientGrantTypes.RefreshToken],
                [],
                [],
                ["openid", "profile", "offline_access", "kinxter.api"]));

        Assert.True(created.Success);
        Assert.Null(created.ClientSecret);
        Assert.False(created.Client!.ClientSecretConfigured);
        var application = await manager.FindByClientIdAsync("living-room-device");
        Assert.Equal(
            OpenIddictConstants.ClientTypes.Public,
            await manager.GetClientTypeAsync(application!));
        Assert.Contains(
            OpenIddictConstants.Permissions.Endpoints.DeviceAuthorization,
            await manager.GetPermissionsAsync(application!));

        var updated = await service.UpdateClientAsync(
            realm.Id,
            created.Client.Id,
            new AuthAdminUpdateClientCommand(
                "Living room backend",
                true,
                AuthClientType.Confidential,
                [AuthClientGrantTypes.ClientCredentials],
                [],
                [],
                ["kinxter.api"]));

        Assert.True(updated.Success);
        Assert.NotNull(updated.ClientSecret);
        application = await manager.FindByClientIdAsync("living-room-device");
        Assert.Equal(
            OpenIddictConstants.ClientTypes.Confidential,
            await manager.GetClientTypeAsync(application!));
        Assert.True(await manager.ValidateClientSecretAsync(application!, updated.ClientSecret!));
        Assert.Contains(
            OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
            await manager.GetPermissionsAsync(application!));

        var changedBackToPublic = await service.UpdateClientAsync(
            realm.Id,
            created.Client.Id,
            new AuthAdminUpdateClientCommand(
                "Living room device",
                true,
                AuthClientType.Public,
                [AuthClientGrantTypes.DeviceCode],
                [],
                [],
                ["openid", "kinxter.api"]));

        Assert.True(changedBackToPublic.Success);
        Assert.Null(changedBackToPublic.ClientSecret);
        Assert.False(changedBackToPublic.Client!.ClientSecretConfigured);
        application = await manager.FindByClientIdAsync("living-room-device");
        Assert.Equal(
            OpenIddictConstants.ClientTypes.Public,
            await manager.GetClientTypeAsync(application!));
        Assert.False(await manager.ValidateClientSecretAsync(application!, updated.ClientSecret!));

        var rotation = await service.RotateSecretAsync(realm.Id, created.Client.Id);

        Assert.False(rotation.Success);
        Assert.Contains("Public clients", rotation.Error);
    }

    [Fact]
    public async Task Backoffice_user_management_invites_assigns_roles_and_revokes_access_on_change()
    {
        var services = new ServiceCollection();
        var now = new DateTimeOffset(2026, 7, 26, 13, 0, 0, TimeSpan.Zero);
        services.AddLogging();
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("D"));
            options.UseOpenIddict();
        });
        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>());
        services
            .AddIdentity<AuthUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();
        services.AddSingleton<IClock>(new TestClock(now));
        services.AddScoped<BackofficeUserAdministrationService>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        dbContext.Database.EnsureCreated();
        var realm = new AuthRealm
        {
            Id = Guid.CreateVersion7(now),
            Name = AuthRealmNames.Backoffice,
            Issuer = "https://auth.example/realms/backoffice",
            PathBase = "/realms/backoffice",
            MfaPolicy = AuthMfaPolicy.Required,
            SignupEnabled = false,
            CreatedAt = now
        };
        dbContext.AuthRealms.Add(realm);
        await dbContext.SaveChangesAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in AuthRoles.AllNames)
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            Assert.True(roleResult.Succeeded);
        }

        var service = scope.ServiceProvider.GetRequiredService<BackofficeUserAdministrationService>();
        var invitation = await service.InviteAsync(
            realm.Id,
            new AuthAdminInviteUserCommand("operator@example.com", [AuthRoles.Operations]));

        Assert.True(invitation.Success, invitation.Error);
        Assert.NotNull(invitation.User);
        Assert.Contains("/realms/backoffice/account/activate", invitation.InvitationUrl);
        Assert.Equal([AuthRoles.Operations], invitation.User!.Roles);
        Assert.True(invitation.User.InvitationPending);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
        var user = await userManager.FindByIdAsync(invitation.User.UserId.ToString("D"));
        Assert.NotNull(user);
        Assert.False(await userManager.HasPasswordAsync(user!));
        var originalSecurityStamp = user!.SecurityStamp;
        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var refreshToken = await tokenManager.CreateAsync(new OpenIddictTokenDescriptor
        {
            Subject = user.Id.ToString("D"),
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypeHints.RefreshToken
        });

        var rolesChanged = await service.UpdateRolesAsync(
            realm.Id,
            user.Id,
            [AuthRoles.Support]);

        Assert.True(rolesChanged.Success, rolesChanged.Error);
        Assert.Equal([AuthRoles.Support], await userManager.GetRolesAsync(user));
        Assert.True(await tokenManager.HasStatusAsync(refreshToken, OpenIddictConstants.Statuses.Revoked));
        user = await userManager.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotEqual(originalSecurityStamp, user!.SecurityStamp);

        var disabled = await service.SetEnabledAsync(realm.Id, user.Id, enabled: false);

        Assert.True(disabled.Success, disabled.Error);
        user = await userManager.FindByIdAsync(user.Id.ToString("D"));
        Assert.Equal(now, user!.DisabledAt);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("D"))
            .Options;

        return new AuthDbContext(options);
    }

    private static AuthRealmRegistry CreateRegistry()
    {
        return new AuthRealmRegistry(new AuthServerOptions
        {
            DbSchema = "auth",
            Realms =
            [
                new AuthOptions
                {
                    Realm = "public",
                    Issuer = "https://auth.example/realms/public",
                    PathBase = "/realms/public"
                }
            ]
        });
    }

    private static AuthRealm CreateRealm()
    {
        return new AuthRealm
        {
            Id = Guid.CreateVersion7(),
            Name = "public",
            Issuer = "https://auth.example/realms/public",
            PathBase = "/realms/public",
            MfaPolicy = AuthMfaPolicy.OptionalStepUp,
            SignupEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
