using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> LoginAsync(
        HttpContext context,
        AuthDbContext dbContext,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        AuthOptions options,
        AuthPageRenderer renderer,
        CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString());

        var user = await userManager.FindByEmailInRealmAsync(dbContext, options, email, cancellationToken);

        if (user is null || user.Realm != options.Realm || user.DeletedAt is not null || user.DisabledAt is not null)
        {
            return await renderer.LoginAsync(context, options, returnUrl, "Invalid credentials.");
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            return Results.Redirect(
                $"{BuildAccountPath(context, "/account/login-2fa")}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (result.Succeeded)
        {
            if (options.RequiresMfa && !user.TwoFactorEnabled)
            {
                return Results.Redirect(
                    $"{BuildAccountPath(context, "/account/manage/totp")}?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            return Results.Redirect(returnUrl);
        }

        return await renderer.LoginAsync(context, options, returnUrl, "Invalid credentials.");
    }
}
