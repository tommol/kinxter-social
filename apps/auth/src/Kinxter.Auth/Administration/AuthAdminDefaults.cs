namespace Kinxter.Auth.Administration;

internal static class AuthAdminDefaults
{
    public const string AuthenticationScheme = "Kinxter.Auth.ControlPlane";

    public const string AuthorizationPolicy = "Kinxter.Auth.ControlPlane.Access";

    public const string AccessClaim = "kinxter.auth.control_plane";

    public const string LoginRateLimitPolicy = "Kinxter.Auth.ControlPlane.Login";
}
