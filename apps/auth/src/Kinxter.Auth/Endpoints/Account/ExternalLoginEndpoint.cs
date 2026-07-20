using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static async Task<IResult> ExternalLoginAsync(
        string provider,
        HttpContext context,
        SignInManager<AuthUser> signInManager,
        AuthOptions options,
        CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var returnUrl = NormalizeReturnUrl(form["returnUrl"].ToString());
        var externalProvider = GetConfiguredExternalProvider(options, provider);

        if (externalProvider is null)
        {
            return Results.Content(
                AuthHtml.Login(options, returnUrl, "External login provider is not available."),
                "text/html");
        }

        var callbackPath = BuildExternalCallbackPath(context, returnUrl, link: false);
        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            externalProvider.AuthenticationScheme,
            callbackPath);

        return Results.Challenge(
            properties,
            authenticationSchemes: [externalProvider.AuthenticationScheme]);
    }
}
