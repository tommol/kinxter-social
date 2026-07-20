using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

internal static partial class OpenIddictEndpoints
{
    private static async Task<IResult> AuthorizeAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        IOpenIddictScopeManager scopeManager,
        AuthOptions authOptions)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (result is not { Succeeded: true } ||
            request.HasPromptValue(PromptValues.Login) ||
            request.MaxAge is 0 ||
            (request.MaxAge is not null &&
                result.Properties?.IssuedUtc is not null &&
                TimeProvider.System.GetUtcNow() - result.Properties.IssuedUtc > TimeSpan.FromSeconds(request.MaxAge.Value)))
        {
            if (request.HasPromptValue(PromptValues.None))
            {
                return Results.Forbid(
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                    }));
            }

            return Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = context.Request.GetEncodedUrl()
            });
        }

        var user = await userManager.GetUserAsync(result.Principal);

        if (user is null || user.Realm != authOptions.Realm || user.DeletedAt is not null || user.DisabledAt is not null)
        {
            await signInManager.SignOutAsync();

            return Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = context.Request.GetEncodedUrl()
            });
        }

        if (authOptions.RequiresMfa && !user.TwoFactorEnabled)
        {
            return Results.Redirect($"/account/manage/totp?returnUrl={Uri.EscapeDataString(context.Request.GetEncodedUrl())}");
        }

        var principal = await CreatePrincipalAsync(user, userManager, scopeManager, request.GetScopes(), authOptions);

        return Results.SignIn(
            principal,
            properties: null,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
