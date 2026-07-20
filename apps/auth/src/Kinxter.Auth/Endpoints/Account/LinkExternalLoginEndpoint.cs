using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> LinkExternalLoginAsync(
        string provider,
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        AuthOptions options,
        CancellationToken cancellationToken)
    {
        var externalProvider = GetConfiguredExternalProvider(options, provider);

        if (externalProvider is null)
        {
            return Results.BadRequest(new { error = "External login provider is not available." });
        }

        var user = await userManager.GetUserAsync(context.User);

        if (user is null || user.Realm != options.Realm || user.DeletedAt is not null || user.DisabledAt is not null)
        {
            return Results.Challenge();
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString());
        var callbackPath = BuildExternalCallbackPath(context, returnUrl, link: true);
        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            externalProvider.AuthenticationScheme,
            callbackPath,
            user.Id.ToString("D"));

        return Results.Challenge(
            properties,
            authenticationSchemes: [externalProvider.AuthenticationScheme]);
    }
}
