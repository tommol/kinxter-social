using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

internal static partial class OpenIddictEndpoints
{
    private static async Task<IResult> ExchangeAsync(
        HttpContext context,
        UserManager<AuthUser> userManager,
        SignInManager<AuthUser> signInManager,
        IOpenIddictScopeManager scopeManager,
        AuthDbContext dbContext,
        AuthOptions authOptions)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified grant type is not supported."
                }));
        }

        if (!await IsClientEnabledForRealmAsync(
                dbContext,
                request.ClientId,
                authOptions,
                context.RequestAborted))
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client is not enabled for this realm."
                }));
        }

        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var subject = result.Principal?.GetClaim(Claims.Subject);
        var user = string.IsNullOrWhiteSpace(subject)
            ? null
            : await userManager.FindByIdAsync(subject);

        if (user is null || user.Realm != authOptions.Realm || !await signInManager.CanSignInAsync(user))
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }));
        }

        var principal = await CreatePrincipalAsync(
            user,
            userManager,
            scopeManager,
            result.Principal!.GetScopes(),
            authOptions);

        return Results.SignIn(
            principal,
            properties: null,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
