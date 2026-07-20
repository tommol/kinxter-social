using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

internal static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddKinxterAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        AuthServerOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(authOptions);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        var requiresHttps = authOptions.Realms.All(realm =>
            Uri.TryCreate(realm.Issuer, UriKind.Absolute, out var issuerUri) &&
            issuerUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        var strictestRealm = authOptions.Realms.Any(realm => realm.RequiresMfa);

        services.AddSingleton(authOptions);
        services.AddHttpContextAccessor();
        services.AddControllersWithViews();
        services.AddScoped(services =>
        {
            var context = services.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var realmOptions = context?.GetAuthRealmOptions();

            return realmOptions ?? throw new InvalidOperationException("The auth realm could not be resolved from the current request.");
        });
        services.AddSharedInfrastructure(configuration);

        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseNpgsql(AuthPostgresConnectionString.Build(connectionString, authOptions.DbSchema));
            options.UseOpenIddict();
        });

        services
            .AddIdentity<AuthUser, IdentityRole<Guid>>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = strictestRealm ? 5 : 8;
                options.Password.RequiredLength = strictestRealm ? 12 : 10;
                options.Password.RequireNonAlphanumeric = strictestRealm;
                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = authOptions.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = requiresHttps
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/account/access-denied";
            options.SlidingExpiration = true;
        });

        services.AddConfiguredExternalAuthenticationProviders(authOptions);

        services.Configure<IdentityPasskeyOptions>(options =>
        {
            options.ServerDomain = configuration["Auth:Passkeys:ServerDomain"];
            options.AuthenticatorTimeout = TimeSpan.FromMinutes(3);
            options.ChallengeSize = 64;
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AuthDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                    .SetEndSessionEndpointUris("/connect/logout")
                    .SetPushedAuthorizationEndpointUris("/connect/par")
                    .SetRevocationEndpointUris("/connect/revocation")
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserInfoEndpointUris("/connect/userinfo");

                options.AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange()
                    .AllowRefreshTokenFlow();

                options.RegisterScopes(
                    Scopes.Email,
                    Scopes.Profile,
                    Scopes.Roles,
                    Scopes.OfflineAccess,
                    AuthScopes.KinxterApi,
                    AuthScopes.KinxterAdmin);

                options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(authOptions.EncryptionKey)));
                options.AddDevelopmentSigningCertificate();

                var aspNetCore = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();

                if (!requiresHttps)
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }

                options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.HandleConfigurationRequestContext>(builder =>
                {
                    builder.SetOrder(int.MaxValue);
                    builder.UseInlineHandler(context =>
                    {
                        var httpRequest = context.Transaction.GetHttpRequest();
                        var realmOptions = httpRequest?.HttpContext.GetAuthRealmOptions();

                        if (realmOptions is null)
                        {
                            return default;
                        }

                        context.Issuer = new Uri(realmOptions.Issuer);
                        context.AuthorizationEndpoint = BuildRealmEndpoint(realmOptions, "/connect/authorize");
                        context.TokenEndpoint = BuildRealmEndpoint(realmOptions, "/connect/token");
                        context.EndSessionEndpoint = BuildRealmEndpoint(realmOptions, "/connect/logout");
                        context.PushedAuthorizationEndpoint = BuildRealmEndpoint(realmOptions, "/connect/par");
                        context.RevocationEndpoint = BuildRealmEndpoint(realmOptions, "/connect/revocation");
                        context.UserInfoEndpoint = BuildRealmEndpoint(realmOptions, "/connect/userinfo");
                        context.JsonWebKeySetEndpoint = BuildRealmEndpoint(realmOptions, "/.well-known/jwks");

                        return default;
                    });
                });
            });

        services.AddAuthorization();
        services.AddScoped<AuthPageRenderer>();
        services.AddScoped<AuthIntegrationEventPublisher>();
        services.AddScoped<ExternalLoginAccountManager>();

        return services;
    }

    private static Uri BuildRealmEndpoint(AuthOptions realmOptions, string path)
    {
        return new Uri($"{realmOptions.Issuer}{path}");
    }

    private static void AddConfiguredExternalAuthenticationProviders(
        this IServiceCollection services,
        AuthServerOptions authOptions)
    {
        var authentication = services.AddAuthentication();

        foreach (var provider in authOptions.Realms.SelectMany(realm => realm.ExternalProviders.EnabledProviders))
        {
            if (!provider.IsConfigured)
            {
                throw new InvalidOperationException(
                    $"External login provider '{provider.Provider}' is enabled but its configuration is incomplete.");
            }
        }

        foreach (var google in authOptions.Realms
            .Select(realm => realm.ExternalProviders.Google)
            .Where(provider => provider.Enabled))
        {
            authentication.AddGoogle(google.AuthenticationScheme, google.DisplayName, options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = google.ClientId;
                options.ClientSecret = google.ClientSecret;
                options.CallbackPath = google.CallbackPath;
                options.SaveTokens = false;

                if (!options.Scope.Contains("email"))
                {
                    options.Scope.Add("email");
                }

                options.ClaimActions.MapUniqueJsonKey("email_verified", "verified_email");
            });
        }

        foreach (var apple in authOptions.Realms
            .Select(realm => realm.ExternalProviders.Apple)
            .Where(provider => provider.Enabled))
        {
            authentication.AddOpenIdConnect(apple.AuthenticationScheme, apple.DisplayName, options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.Authority = "https://appleid.apple.com";
                options.CallbackPath = apple.CallbackPath;
                options.ClientId = apple.ClientId;
                options.ClientSecret = AppleClientSecretFactory.Create(apple);
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = false;
                options.Scope.Clear();
                options.Scope.Add(Scopes.OpenId);
                options.Scope.Add(Scopes.Email);
                options.Scope.Add(Scopes.Profile);
                options.GetClaimsFromUserInfoEndpoint = false;
                options.TokenValidationParameters.NameClaimType = Claims.Name;
            });
        }
    }
}

internal static class AuthScopes
{
    public const string KinxterApi = "kinxter.api";
    public const string KinxterAdmin = "kinxter.admin";
}
