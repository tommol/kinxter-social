using System.Text.RegularExpressions;

namespace Kinxter.Auth;

internal sealed class AuthOptions
{
    private static readonly Regex SchemaNamePattern = new("^[a-z][a-z0-9_]{0,62}$", RegexOptions.Compiled);

    public string Realm { get; init; } = AuthRealms.Public;

    public string Issuer { get; init; } = "http://localhost:8081/realms/public";

    public string PathBase { get; init; } = "/realms/public";

    public string DbSchema { get; init; } = "auth_public";

    public string CookieName { get; init; } = "kinxter-auth-public";

    public AuthMfaPolicy MfaPolicy { get; init; } = AuthMfaPolicy.OptionalStepUp;

    public bool SignupEnabled { get; init; } = true;

    public string EncryptionKey { get; init; } = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";

    public string[] AllowedOrigins { get; init; } = ["http://localhost:3000"];

    public AuthClientOptions[] Clients { get; init; } = [];

    public SeedAdminOptions SeedAdmin { get; init; } = new();

    public bool RequiresMfa => MfaPolicy == AuthMfaPolicy.Required;

    public string IdentityProvider => $"kinxter-auth:{Realm}";

    public static AuthOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Auth");
        var realm = GetString(section, "Realm", AuthRealms.Public).ToLowerInvariant();
        var pathBase = GetPathBase(section, realm);
        var dbSchema = GetString(section, "DbSchema", realm == AuthRealms.Backoffice ? "auth_backoffice" : "auth_public");

        if (!SchemaNamePattern.IsMatch(dbSchema))
        {
            throw new InvalidOperationException($"Auth DB schema '{dbSchema}' is not a safe PostgreSQL schema name.");
        }

        return new AuthOptions
        {
            Realm = realm,
            Issuer = GetString(section, "Issuer", $"http://localhost:{(realm == AuthRealms.Backoffice ? 8082 : 8081)}{pathBase}").TrimEnd('/'),
            PathBase = pathBase,
            DbSchema = dbSchema,
            CookieName = GetString(section, "CookieName", $"kinxter-auth-{realm}"),
            MfaPolicy = GetMfaPolicy(section, realm),
            SignupEnabled = GetBool(section, "SignupEnabled", realm == AuthRealms.Public),
            EncryptionKey = GetString(section, "EncryptionKey", "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY="),
            AllowedOrigins = section.GetSection("AllowedOrigins").Get<string[]>() ??
                (realm == AuthRealms.Backoffice ? ["http://localhost:3001"] : ["http://localhost:3000"]),
            Clients = GetClients(section, realm),
            SeedAdmin = GetSeedAdmin(section.GetSection("SeedAdmin"))
        };
    }

    private static AuthClientOptions[] GetClients(IConfiguration section, string realm)
    {
        var clients = section.GetSection("Clients").Get<AuthClientOptions[]>();

        if (clients is { Length: > 0 })
        {
            return clients;
        }

        return realm == AuthRealms.Backoffice
            ? [
                new AuthClientOptions
                {
                    ClientId = "kinxter-admin",
                    ClientSecret = "kinxter-admin-dev-secret",
                    DisplayName = "Kinxter Admin",
                    RedirectUris = ["http://localhost:3001/api/auth/callback/kinxter"],
                    PostLogoutRedirectUris = ["http://localhost:3001"],
                    Scopes = ["openid", "profile", "email", "offline_access", "roles", "kinxter.admin"]
                }
            ]
            : [
                new AuthClientOptions
                {
                    ClientId = "kinxter-web",
                    ClientSecret = "kinxter-web-dev-secret",
                    DisplayName = "Kinxter Web",
                    RedirectUris = ["http://localhost:3000/api/auth/callback/kinxter"],
                    PostLogoutRedirectUris = ["http://localhost:3000"],
                    Scopes = ["openid", "profile", "email", "offline_access", "kinxter.api"]
                }
            ];
    }

    private static SeedAdminOptions GetSeedAdmin(IConfiguration section)
    {
        return new SeedAdminOptions
        {
            Email = section["Email"],
            Password = section["Password"],
            Enabled = GetBool(section, "Enabled", false)
        };
    }

    private static string GetPathBase(IConfiguration section, string realm)
    {
        var value = GetString(section, "PathBase", realm == AuthRealms.Backoffice ? "/realms/backoffice" : "/realms/public");

        return value.StartsWith("/", StringComparison.Ordinal)
            ? value.TrimEnd('/')
            : $"/{value.TrimEnd('/')}";
    }

    private static AuthMfaPolicy GetMfaPolicy(IConfiguration section, string realm)
    {
        var fallback = realm == AuthRealms.Backoffice
            ? AuthMfaPolicy.Required
            : AuthMfaPolicy.OptionalStepUp;
        var value = section["MfaPolicy"];

        return Enum.TryParse<AuthMfaPolicy>(value, ignoreCase: true, out var policy)
            ? policy
            : fallback;
    }

    private static string GetString(IConfiguration section, string key, string fallback)
    {
        var value = section[key];

        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static bool GetBool(IConfiguration section, string key, bool fallback)
    {
        return bool.TryParse(section[key], out var value)
            ? value
            : fallback;
    }
}

internal static class AuthRealms
{
    public const string Public = "public";
    public const string Backoffice = "backoffice";
}

internal enum AuthMfaPolicy
{
    OptionalStepUp = 1,
    Required = 2
}

internal sealed class AuthClientOptions
{
    public string ClientId { get; init; } = "";

    public string ClientSecret { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public string[] RedirectUris { get; init; } = [];

    public string[] PostLogoutRedirectUris { get; init; } = [];

    public string[] Scopes { get; init; } = [];
}

internal sealed class SeedAdminOptions
{
    public bool Enabled { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }
}
