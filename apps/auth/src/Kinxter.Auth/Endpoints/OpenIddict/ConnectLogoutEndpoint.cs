using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Server.AspNetCore;

namespace Kinxter.Auth;

internal static partial class OpenIddictEndpoints
{
    private static async Task<IResult> LogoutAsync(
        SignInManager<AuthUser> signInManager)
    {
        await signInManager.SignOutAsync();

        return Results.SignOut(
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            },
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }
}
