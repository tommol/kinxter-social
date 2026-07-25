using Kinxter.Auth.Administration;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kinxter.Auth.Tests;

public sealed class AuthAdministrationTests
{
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
