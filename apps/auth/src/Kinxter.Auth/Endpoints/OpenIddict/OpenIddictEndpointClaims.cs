using System.Collections.Immutable;
using System.Security.Claims;
using Kinxter.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

internal static partial class OpenIddictEndpoints
{
    private static async Task<ClaimsPrincipal> CreatePrincipalAsync(
        AuthUser user,
        UserManager<AuthUser> userManager,
        IOpenIddictScopeManager scopeManager,
        IEnumerable<string> scopes,
        AuthOptions authOptions)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);
        var roles = await userManager.GetRolesAsync(user);

        identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
            .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user))
            .SetClaim(Claims.PreferredUsername, await userManager.GetUserNameAsync(user))
            .SetClaim("realm", authOptions.Realm)
            .SetClaim("email_verified", user.EmailConfirmed ? "true" : "false")
            .SetClaims(Claims.Role, roles.ToImmutableArray());

        var principal = new ClaimsPrincipal(identity);
        var requestedScopes = scopes.ToImmutableArray();

        principal.SetScopes(requestedScopes);
        principal.SetResources(await scopeManager.ListResourcesAsync(requestedScopes).ToListAsync());
        principal.SetDestinations(GetDestinations);

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name or Claims.PreferredUsername:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Profile))
                {
                    yield return Destinations.IdentityToken;
                }

                yield break;

            case Claims.Email:
            case "email_verified":
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Email))
                {
                    yield return Destinations.IdentityToken;
                }

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Roles))
                {
                    yield return Destinations.IdentityToken;
                }

                yield break;

            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
