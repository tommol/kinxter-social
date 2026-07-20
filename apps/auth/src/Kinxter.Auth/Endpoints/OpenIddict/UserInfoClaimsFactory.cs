using System.Security.Claims;
using Kinxter.Auth.Infrastructure.Persistence;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

internal static class UserInfoClaimsFactory
{
    public static Dictionary<string, object?> Create(
        AuthUser user,
        ClaimsPrincipal principal,
        IEnumerable<string> roles)
    {
        var claims = new Dictionary<string, object?>
        {
            ["sub"] = user.Id.ToString("D"),
            ["realm"] = user.Realm
        };

        if (principal.HasScope(Scopes.Email))
        {
            claims["email"] = user.Email;
            claims["email_verified"] = user.EmailConfirmed;
        }

        if (principal.HasScope(Scopes.Profile))
        {
            claims["name"] = user.UserName;
            claims["preferred_username"] = user.UserName;
        }

        if (principal.HasScope(Scopes.Roles))
        {
            claims["role"] = roles.ToArray();
        }

        return claims;
    }
}
