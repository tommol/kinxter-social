namespace Kinxter.IntegrationEvents.Identity;

public static class KinxterAuthIdentityProvider
{
    private const string Scheme = "kinxter-auth";

    public static string ForRealm(string realm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(realm);

        return $"{Scheme}:{realm}";
    }
}
