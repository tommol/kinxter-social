using Microsoft.IdentityModel.Tokens;

namespace Kinxter.Api.Authentication;

internal sealed class ApiAuthenticationOptions
{
    public string Audience { get; init; } = "kinxter-api";

    public string PublicIssuer { get; init; } = "http://localhost:8081/realms/public";

    public string BackofficeIssuer { get; init; } = "http://localhost:8082/realms/backoffice";

    public string EncryptionKey { get; init; } = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";

    public static ApiAuthenticationOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Auth");

        return new ApiAuthenticationOptions
        {
            Audience = GetString(section, "Audience", "kinxter-api"),
            PublicIssuer = GetString(section, "PublicIssuer", "http://localhost:8081/realms/public").TrimEnd('/'),
            BackofficeIssuer = GetString(section, "BackofficeIssuer", "http://localhost:8082/realms/backoffice").TrimEnd('/'),
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
}
