using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> LoginTwoFactorAsync(
        HttpContext context,
        SignInManager<AuthUser> signInManager,
        CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var code = CleanAuthenticatorCode(form["code"].ToString());
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString());

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            code,
            isPersistent: false,
            rememberClient: false);

        return result.Succeeded
            ? Results.Redirect(returnUrl)
            : Results.Content(AuthHtml.LoginTwoFactor(returnUrl, "Invalid authenticator code."), "text/html");
    }
}
