using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Kinxter.Api.Authentication;

internal static class ApiAuthenticationExtensions
{
    public static IServiceCollection AddKinxterApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ApiAuthenticationOptions.FromConfiguration(configuration);
        var encryptionKey = options.GetEncryptionKey();

        services.AddSingleton(options);
        services.AddAuthentication()
            .AddJwtBearer(ApiAuthenticationSchemes.PublicRealm, jwt =>
            {
                ConfigureJwtBearer(jwt, options.PublicIssuer, options.Audience, encryptionKey);
            })
            .AddJwtBearer(ApiAuthenticationSchemes.BackofficeRealm, jwt =>
            {
                ConfigureJwtBearer(jwt, options.BackofficeIssuer, options.Audience, encryptionKey);
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(ApiAuthorizationPolicies.PublicUser, policy =>
            {
                policy.AddAuthenticationSchemes(ApiAuthenticationSchemes.PublicRealm);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => HasScope(context, "kinxter.api") && HasRealm(context, options.PublicRealm));
            })
            .AddPolicy(ApiAuthorizationPolicies.BackofficeAdmin, policy =>
            {
                ConfigureBackofficePolicy(policy, options, ApiPermissions.AdminAccess);
            })
            .AddPolicy(ApiAuthorizationPolicies.MonitoringRead, policy =>
            {
                ConfigureBackofficePolicy(policy, options, ApiPermissions.MonitoringRead);
            })
            .AddPolicy(ApiAuthorizationPolicies.ModerationRead, policy =>
            {
                ConfigureBackofficePolicy(policy, options, ApiPermissions.ModerationRead);
            })
            .AddPolicy(ApiAuthorizationPolicies.ModerationWrite, policy =>
            {
                ConfigureBackofficePolicy(policy, options, ApiPermissions.ModerationWrite);
            })
            .AddPolicy(ApiAuthorizationPolicies.UsersRead, policy =>
            {
                ConfigureBackofficePolicy(policy, options, ApiPermissions.UsersRead);
            })
            .AddPolicy(ApiAuthorizationPolicies.UsersManage, policy =>
            {
                ConfigureBackofficePolicy(policy, options, ApiPermissions.UsersManage);
            })
            .AddPolicy(ApiAuthorizationPolicies.AdminUsersManage, policy =>
            {
                ConfigureBackofficePolicy(policy, options, ApiPermissions.AdminUsersManage);
            });

        return services;
    }

    private static void ConfigureJwtBearer(
        JwtBearerOptions options,
        string issuer,
        string audience,
        SecurityKey encryptionKey)
    {
        options.Authority = issuer;
        options.Audience = audience;
        options.RequireHttpsMetadata = issuer.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            TokenDecryptionKey = encryptionKey,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    }

    private static bool HasScope(AuthorizationHandlerContext context, string scope)
    {
        return context.User.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Any(value => string.Equals(value, scope, StringComparison.Ordinal));
    }

    private static bool HasRealm(AuthorizationHandlerContext context, string realm)
    {
        return context.User.Claims.Any(claim =>
            claim.Type == "realm" &&
            string.Equals(claim.Value, realm, StringComparison.Ordinal));
    }

    private static void ConfigureBackofficePolicy(
        AuthorizationPolicyBuilder policy,
        ApiAuthenticationOptions options,
        string permission)
    {
        policy.AddAuthenticationSchemes(ApiAuthenticationSchemes.BackofficeRealm);
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            HasScope(context, "kinxter.admin") &&
            HasRealm(context, options.BackofficeRealm) &&
            context.User.HasClaim("permission", permission));
    }
}
