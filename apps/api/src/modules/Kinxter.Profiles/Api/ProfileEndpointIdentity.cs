using System.Security.Claims;
using Kinxter.IntegrationEvents.Identity;

namespace Kinxter.Profiles.Api;

internal sealed record ProfileEndpointIdentity(
    string IdentityProvider,
    string Subject);

internal static class ProfileEndpointIdentityReader
{
    public static ProfileEndpointIdentity GetIdentity(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated public token does not contain a subject.");
        var realm = principal.FindFirstValue("realm")
            ?? throw new InvalidOperationException("Authenticated public token does not contain a realm.");

        return new ProfileEndpointIdentity(
            KinxterAuthIdentityProvider.ForRealm(realm),
            subject);
    }
}
