using System.Security.Claims;
using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Auth.Rendering.Models;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth.Tests;

public sealed class ExternalAuthTests
{
    [Fact]
    public void AuthOptions_reads_external_provider_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Realm"] = "customers",
                ["Auth:ExternalProviders:Google:Enabled"] = "true",
                ["Auth:ExternalProviders:Google:ClientId"] = "google-client",
                ["Auth:ExternalProviders:Google:ClientSecret"] = "google-secret",
                ["Auth:ExternalProviders:Apple:Enabled"] = "true",
                ["Auth:ExternalProviders:Apple:ClientId"] = "apple-client",
                ["Auth:ExternalProviders:Apple:TeamId"] = "team-id",
                ["Auth:ExternalProviders:Apple:KeyId"] = "key-id",
                ["Auth:ExternalProviders:Apple:PrivateKeyPem"] = "-----BEGIN PRIVATE KEY-----\\nvalue\\n-----END PRIVATE KEY-----"
            })
            .Build();

        var options = AuthOptions.FromConfiguration(configuration);

        Assert.True(options.ExternalProviders.Google.Enabled);
        Assert.True(options.ExternalProviders.Google.IsConfigured);
        Assert.Equal("google-client", options.ExternalProviders.Google.ClientId);
        Assert.True(options.ExternalProviders.Apple.Enabled);
        Assert.True(options.ExternalProviders.Apple.IsConfigured);
        Assert.Equal("team-id", options.ExternalProviders.Apple.TeamId);
    }

    [Fact]
    public void AuthServerOptions_reads_multiple_realms()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:DbSchema"] = "auth",
                ["Auth:Realms:0:Realm"] = "public",
                ["Auth:Realms:0:Issuer"] = "http://localhost:8081/realms/public",
                ["Auth:Realms:0:AllowedOrigins:0"] = "http://localhost:3000",
                ["Auth:Realms:1:Realm"] = "backoffice",
                ["Auth:Realms:1:Issuer"] = "http://localhost:8081/realms/backoffice",
                ["Auth:Realms:1:MfaPolicy"] = "Required",
                ["Auth:Realms:1:SignupEnabled"] = "false",
                ["Auth:Realms:1:AllowedOrigins:0"] = "http://localhost:3001"
            })
            .Build();

        var options = AuthServerOptions.FromConfiguration(configuration);

        Assert.Equal("auth", options.DbSchema);
        Assert.Collection(
            options.Realms,
            publicRealm =>
            {
                Assert.Equal("public", publicRealm.Realm);
                Assert.Equal("/realms/public", publicRealm.PathBase);
                Assert.True(publicRealm.SignupEnabled);
            },
            backofficeRealm =>
            {
                Assert.Equal("backoffice", backofficeRealm.Realm);
                Assert.Equal("/realms/backoffice", backofficeRealm.PathBase);
                Assert.True(backofficeRealm.RequiresMfa);
                Assert.False(backofficeRealm.SignupEnabled);
            });
        Assert.Contains("http://localhost:3000", options.AllowedOrigins);
        Assert.Contains("http://localhost:3001", options.AllowedOrigins);
    }

    [Fact]
    public void AuthServerOptions_reads_named_realm_section_key()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Realms:customers:Issuer"] = "http://localhost:8081/realms/customers"
            })
            .Build();

        var options = AuthServerOptions.FromConfiguration(configuration);
        var realm = Assert.Single(options.Realms);

        Assert.Equal("customers", realm.Realm);
        Assert.Equal("/realms/customers", realm.PathBase);
        Assert.True(realm.SignupEnabled);
    }

    [Fact]
    public void Login_model_omits_external_providers_when_they_are_disabled()
    {
        var model = new AuthLoginPageViewModel(new AuthOptions(), "/connect/authorize");

        Assert.Empty(model.ExternalProviders);
    }

    [Fact]
    public void Login_model_includes_configured_external_providers_with_path_base()
    {
        var options = new AuthOptions
        {
            PathBase = "/realms/public",
            ExternalProviders = new AuthExternalProvidersOptions
            {
                Google = new AuthExternalProviderOptions
                {
                    Provider = AuthExternalProviderNames.Google,
                    DisplayName = "Google",
                    Enabled = true,
                    ClientId = "google-client",
                    ClientSecret = "google-secret"
                }
            }
        };

        var model = new AuthLoginPageViewModel(options, "/connect/authorize");

        var provider = Assert.Single(model.ExternalProviders);
        Assert.Equal("Google", provider.DisplayName);
        Assert.Equal("/realms/public/account/external-login/google", provider.ActionPath);
    }

    [Fact]
    public async Task External_login_with_new_verified_email_creates_user_and_login()
    {
        using var scope = CreateAuthScope();
        var manager = scope.GetRequiredService<ExternalLoginAccountManager>();
        var userManager = scope.GetRequiredService<UserManager<AuthUser>>();
        var publisher = scope.GetRequiredService<CapturingModuleEventPublisher>();

        var result = await manager.ResolveForSignInAsync(CreateLoginInfo());

        Assert.Equal(ExternalLoginAccountStatus.CreatedUser, result.Status);
        Assert.NotNull(result.User);
        Assert.Equal("user@example.com", result.User.Email);
        Assert.True(result.User.EmailConfirmed);
        Assert.NotNull(await userManager.FindByLoginAsync(AuthExternalProviderNames.Google, "google-sub"));
        Assert.Contains(publisher.Published, current => current is IdentityUserRegisteredV1);
    }

    [Fact]
    public async Task External_login_with_existing_linked_login_returns_existing_user()
    {
        using var scope = CreateAuthScope();
        var manager = scope.GetRequiredService<ExternalLoginAccountManager>();
        var userManager = scope.GetRequiredService<UserManager<AuthUser>>();
        var user = await CreateUserAsync(userManager, "user@example.com");
        var login = CreateLoginInfo();

        Assert.True((await userManager.AddLoginAsync(user, login)).Succeeded);

        var result = await manager.ResolveForSignInAsync(login);

        Assert.Equal(ExternalLoginAccountStatus.ExistingLinkedUser, result.Status);
        Assert.Equal(user.Id, result.User?.Id);
    }

    [Fact]
    public async Task External_login_does_not_auto_link_existing_email()
    {
        using var scope = CreateAuthScope();
        var manager = scope.GetRequiredService<ExternalLoginAccountManager>();
        var userManager = scope.GetRequiredService<UserManager<AuthUser>>();

        await CreateUserAsync(userManager, "user@example.com");

        var result = await manager.ResolveForSignInAsync(CreateLoginInfo());

        Assert.Equal(ExternalLoginAccountStatus.EmailAlreadyExists, result.Status);
        Assert.Null(await userManager.FindByLoginAsync(AuthExternalProviderNames.Google, "google-sub"));
    }

    [Fact]
    public async Task External_login_with_disabled_linked_user_is_rejected()
    {
        using var scope = CreateAuthScope();
        var manager = scope.GetRequiredService<ExternalLoginAccountManager>();
        var userManager = scope.GetRequiredService<UserManager<AuthUser>>();
        var user = await CreateUserAsync(userManager, "user@example.com");
        var login = CreateLoginInfo();

        user.DisabledAt = DateTimeOffset.UtcNow;
        Assert.True((await userManager.UpdateAsync(user)).Succeeded);
        Assert.True((await userManager.AddLoginAsync(user, login)).Succeeded);

        var result = await manager.ResolveForSignInAsync(login);

        Assert.Equal(ExternalLoginAccountStatus.UserUnavailable, result.Status);
    }

    [Fact]
    public async Task External_login_without_verified_email_is_rejected()
    {
        using var scope = CreateAuthScope();
        var manager = scope.GetRequiredService<ExternalLoginAccountManager>();

        var result = await manager.ResolveForSignInAsync(CreateLoginInfo(emailVerified: false));

        Assert.Equal(ExternalLoginAccountStatus.EmailNotVerified, result.Status);
    }

    [Fact]
    public async Task Link_rejects_provider_login_already_linked_to_another_user()
    {
        using var scope = CreateAuthScope();
        var manager = scope.GetRequiredService<ExternalLoginAccountManager>();
        var userManager = scope.GetRequiredService<UserManager<AuthUser>>();
        var firstUser = await CreateUserAsync(userManager, "first@example.com");
        var secondUser = await CreateUserAsync(userManager, "second@example.com");
        var login = CreateLoginInfo();

        Assert.True((await userManager.AddLoginAsync(firstUser, login)).Succeeded);

        var result = await manager.LinkAsync(secondUser, login);

        Assert.Equal(ExternalLoginLinkStatus.LinkedToAnotherUser, result.Status);
    }

    [Fact]
    public async Task Link_adds_provider_login_to_current_user()
    {
        using var scope = CreateAuthScope();
        var manager = scope.GetRequiredService<ExternalLoginAccountManager>();
        var userManager = scope.GetRequiredService<UserManager<AuthUser>>();
        var user = await CreateUserAsync(userManager, "user@example.com");
        var login = CreateLoginInfo();

        var result = await manager.LinkAsync(user, login);

        Assert.Equal(ExternalLoginLinkStatus.Linked, result.Status);
        Assert.Equal(user.Id, (await userManager.FindByLoginAsync(AuthExternalProviderNames.Google, "google-sub"))?.Id);
    }

    [Fact]
    public void UserInfo_claims_respect_requested_scopes()
    {
        var user = new AuthUser
        {
            Id = Guid.CreateVersion7(),
            Realm = "public",
            UserName = "user@example.com",
            Email = "user@example.com",
            EmailConfirmed = true
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        principal.SetScopes([Scopes.OpenId]);

        var withoutScopes = UserInfoClaimsFactory.Create(user, principal, ["admin"]);

        Assert.True(withoutScopes.ContainsKey("sub"));
        Assert.False(withoutScopes.ContainsKey("email"));
        Assert.False(withoutScopes.ContainsKey("name"));
        Assert.False(withoutScopes.ContainsKey("role"));

        principal.SetScopes([Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles]);

        var withScopes = UserInfoClaimsFactory.Create(user, principal, ["admin"]);

        Assert.Equal("user@example.com", withScopes["email"]);
        Assert.Equal("user@example.com", withScopes["preferred_username"]);
        Assert.Equal(new[] { "admin" }, Assert.IsAssignableFrom<IReadOnlyCollection<string>>(withScopes["role"]));
    }

    private static AuthTestScope CreateAuthScope()
    {
        var services = new ServiceCollection();
        var authOptions = new AuthOptions
        {
            Realm = "public"
        };

        services.AddLogging();
        services.AddSingleton(authOptions);
        services.AddSingleton<IClock>(new FixedClock(new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero)));
        services.AddSingleton<CapturingModuleEventPublisher>();
        services.AddSingleton<IModuleEventPublisher>(provider =>
            provider.GetRequiredService<CapturingModuleEventPublisher>());
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("D"));
        });
        services
            .AddIdentity<AuthUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<AuthIntegrationEventPublisher>();
        services.AddScoped<ExternalLoginAccountManager>();

        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        dbContext.Database.EnsureCreated();

        return new AuthTestScope(serviceProvider, scope);
    }

    private static async Task<AuthUser> CreateUserAsync(
        UserManager<AuthUser> userManager,
        string email)
    {
        var user = new AuthUser
        {
            Id = Guid.CreateVersion7(),
            Realm = "public",
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var result = await userManager.CreateAsync(user);

        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(error => error.Description)));

        return user;
    }

    private static ExternalLoginInfo CreateLoginInfo(
        string provider = AuthExternalProviderNames.Google,
        string providerKey = "google-sub",
        string email = "user@example.com",
        bool emailVerified = true)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(Claims.Subject, providerKey),
                new Claim(Claims.Email, email),
                new Claim("email_verified", emailVerified ? "true" : "false")
            ],
            provider);
        var principal = new ClaimsPrincipal(identity);

        return new ExternalLoginInfo(principal, provider, providerKey, provider);
    }

    private sealed class AuthTestScope : IDisposable
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IServiceScope scope;

        public AuthTestScope(ServiceProvider serviceProvider, IServiceScope scope)
        {
            this.serviceProvider = serviceProvider;
            this.scope = scope;
        }

        public T GetRequiredService<T>()
            where T : notnull
        {
            return this.scope.ServiceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
            this.scope.Dispose();
            this.serviceProvider.Dispose();
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class CapturingModuleEventPublisher : IModuleEventPublisher
    {
        public List<IModuleEvent> Published { get; } = [];

        public Task PublishAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken = default)
        {
            Published.Add(moduleEvent);

            return Task.CompletedTask;
        }

        public Task PublishAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
            where TEvent : IModuleEvent
        {
            return PublishAsync((IModuleEvent)moduleEvent, cancellationToken);
        }
    }
}
