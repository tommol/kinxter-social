using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

internal static partial class OpenIddictEndpoints
{
    private static async Task<IResult> UserInfoAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        AuthOptions authOptions)
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var subject = result.Principal?.GetClaim(Claims.Subject);
        var user = string.IsNullOrWhiteSpace(subject)
            ? null
            : await userManager.FindByIdAsync(subject);

        if (user is null || user.Realm != authOptions.Realm)
        {
            return Results.Challenge(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var roles = await userManager.GetRolesAsync(user);
        var claims = UserInfoClaimsFactory.Create(user, result.Principal!, roles);

        return Results.Ok(claims);
    }
}
