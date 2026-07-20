using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ExternalLoginCallbackAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        ExternalLoginAccountManager externalLogins,
        AuthOptions options,
        string? returnUrl,
        bool? link,
        CancellationToken cancellationToken)
    {
        var normalizedReturnUrl = NormalizeReturnUrl(returnUrl);
        var isLinkFlow = link is true;
        var currentUser = isLinkFlow
            ? await userManager.GetUserAsync(context.User)
            : null;

        if (isLinkFlow && currentUser is null)
        {
            return Results.Challenge();
        }

        var expectedXsrf = currentUser?.Id.ToString("D");
        var login = await signInManager.GetExternalLoginInfoAsync(expectedXsrf);

        if (login is null)
        {
            return LoginError(options, normalizedReturnUrl, "External login could not be completed.");
        }

        try
        {
            var externalProvider = GetConfiguredExternalProvider(options, login.LoginProvider);

            if (externalProvider is null)
            {
                return LoginError(options, normalizedReturnUrl, "External login provider is not available.");
            }

            return isLinkFlow
                ? await CompleteExternalLinkAsync(
                    currentUser,
                    login,
                    externalLogins,
                    options,
                    normalizedReturnUrl)
                : await CompleteExternalSignInAsync(
                    login,
                    externalLogins,
                    signInManager,
                    options,
                    context,
                    normalizedReturnUrl,
                    cancellationToken);
        }
        finally
        {
            await context.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    private static async Task<IResult> CompleteExternalSignInAsync(
        ExternalLoginInfo login,
        ExternalLoginAccountManager externalLogins,
        SignInManager<AuthUser> signInManager,
        AuthOptions options,
        HttpContext context,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var accountResult = await externalLogins.ResolveForSignInAsync(login, cancellationToken);

        return accountResult.Status switch
        {
            ExternalLoginAccountStatus.ExistingLinkedUser => await SignInExistingExternalUserAsync(
                accountResult.User!,
                login,
                signInManager,
                options,
                context,
                returnUrl),
            ExternalLoginAccountStatus.CreatedUser => await SignInNewExternalUserAsync(
                accountResult.User!,
                login,
                signInManager,
                options,
                context,
                returnUrl),
            ExternalLoginAccountStatus.UserUnavailable => LoginError(
                options,
                returnUrl,
                "This account cannot sign in."),
            ExternalLoginAccountStatus.EmailNotVerified => LoginError(
                options,
                returnUrl,
                "External provider did not return a verified email address."),
            ExternalLoginAccountStatus.EmailAlreadyExists => LoginError(
                options,
                returnUrl,
                "An account with this email already exists. Sign in with email and link this provider from your account."),
            _ => LoginError(
                options,
                returnUrl,
                accountResult.Error ?? "External login failed.")
        };
    }

    private static async Task<IResult> CompleteExternalLinkAsync(
        AuthUser? currentUser,
        ExternalLoginInfo login,
        ExternalLoginAccountManager externalLogins,
        AuthOptions options,
        string returnUrl)
    {
        if (currentUser is null)
        {
            return Results.Challenge();
        }

        var linkResult = await externalLogins.LinkAsync(currentUser, login);

        return linkResult.Status switch
        {
            ExternalLoginLinkStatus.Linked or ExternalLoginLinkStatus.AlreadyLinked => Results.Redirect(returnUrl),
            ExternalLoginLinkStatus.LinkedToAnotherUser => Results.Conflict(new
            {
                error = "External login is already linked to another account."
            }),
            ExternalLoginLinkStatus.UserUnavailable => Results.Challenge(),
            _ => Results.BadRequest(new { error = linkResult.Error ?? "External login could not be linked." })
        };
    }

    private static async Task<IResult> SignInExistingExternalUserAsync(
        AuthUser user,
        ExternalLoginInfo login,
        SignInManager<AuthUser> signInManager,
        AuthOptions options,
        HttpContext context,
        string returnUrl)
    {
        var signInResult = await signInManager.ExternalLoginSignInAsync(
            login.LoginProvider,
            login.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: false);

        if (signInResult.RequiresTwoFactor)
        {
            return Results.Redirect(
                $"{BuildAccountPath(context, "/account/login-2fa")}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (!signInResult.Succeeded)
        {
            return LoginError(options, returnUrl, "External login failed.");
        }

        return options.RequiresMfa && !user.TwoFactorEnabled
            ? Results.Redirect(
                $"{BuildAccountPath(context, "/account/manage/totp")}?returnUrl={Uri.EscapeDataString(returnUrl)}")
            : Results.Redirect(returnUrl);
    }

    private static async Task<IResult> SignInNewExternalUserAsync(
        AuthUser user,
        ExternalLoginInfo login,
        SignInManager<AuthUser> signInManager,
        AuthOptions options,
        HttpContext context,
        string returnUrl)
    {
        await signInManager.SignInAsync(user, isPersistent: false, login.LoginProvider);

        return options.RequiresMfa
            ? Results.Redirect(
                $"{BuildAccountPath(context, "/account/manage/totp")}?returnUrl={Uri.EscapeDataString(returnUrl)}")
            : Results.Redirect(returnUrl);
    }

    private static IResult LoginError(AuthOptions options, string returnUrl, string error)
    {
        return Results.Content(AuthHtml.Login(options, returnUrl, error), "text/html");
    }
}
