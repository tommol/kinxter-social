using System.Security.Claims;
using System.Threading.RateLimiting;
using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth.Administration;

internal static class AuthAdminServiceCollectionExtensions
{
    public static IServiceCollection AddAuthAdministration(
        this IServiceCollection services,
        AuthAdminOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddScoped<AuthAdministrationService>();
        services.AddScoped<AuthClientAdministrationService>();
        services.AddScoped<BackofficeUserAdministrationService>();
        services.AddScoped<AuthAdminPageRenderer>();
        services.AddSingleton<IPasswordHasher<AuthAdministrator>, PasswordHasher<AuthAdministrator>>();

        services.AddAntiforgery(antiforgery =>
        {
            antiforgery.Cookie.Name = $"{options.CookieName}-xsrf";
            antiforgery.Cookie.HttpOnly = true;
            antiforgery.Cookie.IsEssential = true;
            antiforgery.Cookie.SameSite = SameSiteMode.Strict;
            antiforgery.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            antiforgery.FormFieldName = "__RequestVerificationToken";
        });

        services
            .AddAuthentication()
            .AddCookie(AuthAdminDefaults.AuthenticationScheme, cookie =>
            {
                cookie.Cookie.Name = options.CookieName;
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.IsEssential = true;
                cookie.Cookie.SameSite = SameSiteMode.Strict;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                cookie.ExpireTimeSpan = TimeSpan.FromHours(options.SessionHours);
                cookie.LoginPath = options.LoginPath;
                cookie.SlidingExpiration = true;
                cookie.Events.OnValidatePrincipal = ValidateAdministratorAsync;
            });

        services.AddAuthorization(authorization =>
        {
            authorization.AddPolicy(AuthAdminDefaults.AuthorizationPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(AuthAdminDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(AuthAdminDefaults.AccessClaim, bool.TrueString);
            });
        });

        services.AddRateLimiter(rateLimiter =>
        {
            rateLimiter.AddPolicy(
                AuthAdminDefaults.LoginRateLimitPolicy,
                context =>
                {
                    var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });
            rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }

    private static async Task ValidateAdministratorAsync(
        Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext context)
    {
        var administratorId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(administratorId, out var parsedAdministratorId))
        {
            context.RejectPrincipal();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AuthDbContext>();
        var administratorIsActive = await dbContext.AuthAdministrators
            .AsNoTracking()
            .AnyAsync(administrator =>
                administrator.Id == parsedAdministratorId &&
                administrator.Enabled);

        if (!administratorIsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AuthAdminDefaults.AuthenticationScheme);
        }
    }
}
