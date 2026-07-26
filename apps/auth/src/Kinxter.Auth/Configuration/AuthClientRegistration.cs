using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Kinxter.Auth;

public enum AuthClientType
{
    Public = 1,
    Confidential = 2
}

public static class AuthClientGrantTypes
{
    public const string AuthorizationCode = GrantTypes.AuthorizationCode;
    public const string RefreshToken = GrantTypes.RefreshToken;
    public const string ClientCredentials = GrantTypes.ClientCredentials;
    public const string DeviceCode = GrantTypes.DeviceCode;

    public static string[] Default => [AuthorizationCode, RefreshToken];
}
