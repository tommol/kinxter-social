using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> LoginWithPasskeyAsync(
        HttpContext context,
        SignInManager<AuthUser> signInManager)
    {
        var credentialJson = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var result = await signInManager.PasskeySignInAsync(credentialJson);

        return result.Succeeded
            ? Results.Ok(new { status = "signed-in" })
            : Results.Unauthorized();
    }
}
