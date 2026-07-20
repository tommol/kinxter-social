using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> EnableTotpAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return Results.Challenge();
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var code = CleanAuthenticatorCode(form["code"].ToString());
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString());
        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!valid)
        {
            var key = await userManager.GetAuthenticatorKeyAsync(user);

            return Results.Content(AuthHtml.TotpSetup(key, returnUrl, "Invalid authenticator code."), "text/html");
        }

        await userManager.SetTwoFactorEnabledAsync(user, enabled: true);

        return Results.Redirect(returnUrl);
    }
}
