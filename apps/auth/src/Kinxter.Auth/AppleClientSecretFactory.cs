using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Kinxter.Auth;

internal static class AppleClientSecretFactory
{
    public static string Create(AuthAppleExternalProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsConfigured)
        {
            throw new InvalidOperationException("Apple external login is enabled but its configuration is incomplete.");
        }

        using var algorithm = ECDsa.Create();
        algorithm.ImportFromPem(NormalizePrivateKey(options.PrivateKeyPem));

        var now = DateTime.UtcNow;
        var key = new ECDsaSecurityKey(algorithm)
        {
            KeyId = options.KeyId
        };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.TeamId,
            Audience = "https://appleid.apple.com",
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, options.ClientId)]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddDays(180),
            SigningCredentials = credentials
        };
        var handler = new JwtSecurityTokenHandler();

        return handler.CreateEncodedJwt(descriptor);
    }

    private static ReadOnlySpan<char> NormalizePrivateKey(string privateKey)
    {
        return privateKey.Replace("\\n", "\n", StringComparison.Ordinal).AsSpan();
    }
}
