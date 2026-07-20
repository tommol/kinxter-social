using Kinxter.IntegrationEvents.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Kinxter.Api.Authentication;

internal sealed class ApiAuthenticationOptions
{
    public string Audience { get; init; } = "kinxter-api";

    public string PublicIssuer { get; init; } = "";

    public string BackofficeIssuer { get; init; } = "";

    public string PublicRealm { get; init; } = "";

    public string BackofficeRealm { get; init; } = "";

    public string EncryptionKey { get; init; } = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";

    public string PublicIdentityProvider => KinxterAuthIdentityProvider.ForRealm(PublicRealm);

    public static ApiAuthenticationOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Auth");
        var publicIssuer = GetRequiredString(section, "PublicIssuer").TrimEnd('/');
        var backofficeIssuer = GetRequiredString(section, "BackofficeIssuer").TrimEnd('/');

        return new ApiAuthenticationOptions
        {
            Audience = GetString(section, "Audience", "kinxter-api"),
            PublicIssuer = publicIssuer,
            BackofficeIssuer = backofficeIssuer,
            PublicRealm = NormalizeRealm(GetString(section, "PublicRealm", InferRealmFromIssuer(publicIssuer))),
            BackofficeRealm = NormalizeRealm(GetString(section, "BackofficeRealm", InferRealmFromIssuer(backofficeIssuer))),
            EncryptionKey = GetString(section, "EncryptionKey", "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")
        };
    }

    public SecurityKey GetEncryptionKey()
    {
        return new SymmetricSecurityKey(Convert.FromBase64String(EncryptionKey));
    }

    private static string GetString(IConfiguration section, string key, string fallback)
    {
        var value = section[key];

        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static string GetRequiredString(IConfiguration section, string key)
    {
        var value = section[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Auth:{key} must be configured.");
        }

        return value.Trim();
    }

    private static string InferRealmFromIssuer(string issuer)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Auth issuer '{issuer}' is not a valid absolute URI.");
        }

        var realm = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(realm))
        {
            throw new InvalidOperationException($"Auth issuer '{issuer}' does not include a realm path segment.");
        }

        return NormalizeRealm(realm);
    }

    private static string NormalizeRealm(string realm)
    {
        if (string.IsNullOrWhiteSpace(realm))
        {
            throw new InvalidOperationException("Auth realm must be configured.");
        }

        return realm.Trim().ToLowerInvariant();
    }
}
