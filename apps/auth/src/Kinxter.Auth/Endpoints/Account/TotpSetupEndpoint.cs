using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> TotpSetupAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        string? returnUrl)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return Results.Challenge();
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrWhiteSpace(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        return Results.Content(AuthHtml.TotpSetup(key, NormalizeReturnUrl(returnUrl)), "text/html");
    }
}
