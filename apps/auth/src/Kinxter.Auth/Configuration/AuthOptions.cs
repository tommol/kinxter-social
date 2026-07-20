using System.Text.RegularExpressions;
using Kinxter.IntegrationEvents.Identity;

namespace Kinxter.Auth;

internal sealed class AuthServerOptions
{
    public string DbSchema { get; init; } = "auth";

    public string CookieName { get; init; } = "kinxter-auth";

    public string EncryptionKey { get; init; } = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";

    public AuthOptions[] Realms { get; init; } = [];

    public string[] AllowedOrigins =>
        Realms
            .SelectMany(realm => realm.AllowedOrigins)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public AuthOptions DefaultRealm =>
        Realms.Length > 0
            ? Realms[0]
            : throw new InvalidOperationException("At least one auth realm must be configured.");

    public static AuthServerOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Auth");
        var realms = GetConfiguredRealms(section).ToArray();

        if (realms.Length == 0 && !string.IsNullOrWhiteSpace(section["Realm"]))
        {
            realms = [AuthOptions.FromSection(section)];
        }

        var options = new AuthServerOptions
        {
            DbSchema = AuthOptions.GetString(section, "DbSchema", "auth"),
            CookieName = AuthOptions.GetString(section, "CookieName", "kinxter-auth"),
            EncryptionKey = AuthOptions.GetString(
                section,
                "EncryptionKey",
                "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY="),
            Realms = realms
        };

        Validate(options);

        return options;
    }

    public bool TryFindByPath(PathString path, out AuthOptions options, out PathString remaining)
    {
        foreach (var realm in Realms.OrderByDescending(realm => realm.PathBase.Length))
        {
            if (path.StartsWithSegments(new PathString(realm.PathBase), out remaining))
            {
                options = realm;

                return true;
            }
        }

        options = null!;
        remaining = PathString.Empty;

        return false;
    }

    public bool TryFindByRealm(string realm, out AuthOptions options)
    {
        options = Realms.SingleOrDefault(current =>
            string.Equals(current.Realm, realm, StringComparison.OrdinalIgnoreCase))!;

        return options is not null;
    }

    private static IEnumerable<AuthOptions> GetConfiguredRealms(IConfiguration section)
    {
        var realmSections = section.GetSection("Realms").GetChildren().ToArray();

        foreach (var realmSection in realmSections)
        {
            var realmFallback = int.TryParse(realmSection.Key, out _)
                ? null
                : realmSection.Key;

            yield return AuthOptions.FromSection(realmSection, realmFallback);
        }
    }

    private static void Validate(AuthServerOptions options)
    {
        if (!AuthOptions.IsSafeSchemaName(options.DbSchema))
        {
            throw new InvalidOperationException($"Auth DB schema '{options.DbSchema}' is not a safe PostgreSQL schema name.");
        }

        if (options.Realms.Length == 0)
        {
            throw new InvalidOperationException("At least one auth realm must be configured.");
        }

        var duplicateRealms = options.Realms
            .GroupBy(realm => realm.Realm, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateRealms.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate auth realms are configured: {string.Join(", ", duplicateRealms)}.");
        }

        var duplicatePathBases = options.Realms
            .GroupBy(realm => realm.PathBase, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicatePathBases.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate auth realm path bases are configured: {string.Join(", ", duplicatePathBases)}.");
        }

        var duplicateClients = options.Realms
            .SelectMany(realm => realm.Clients)
            .GroupBy(client => client.ClientId.Trim(), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateClients.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate auth client ids are configured: {string.Join(", ", duplicateClients)}.");
        }

        var clientsWithoutId = options.Realms
            .Where(realm => realm.Clients.Any(client => string.IsNullOrWhiteSpace(client.ClientId)))
            .Select(realm => realm.Realm)
            .ToArray();

        if (clientsWithoutId.Length > 0)
        {
            throw new InvalidOperationException($"Auth clients without client ids are configured in realms: {string.Join(", ", clientsWithoutId)}.");
        }
    }
}

internal sealed class AuthOptions
{
    private static readonly Regex SchemaNamePattern = new("^[a-z][a-z0-9_]{0,62}$", RegexOptions.Compiled);
    private static readonly Regex RealmNamePattern = new("^[a-z0-9][a-z0-9_.-]{0,63}$", RegexOptions.Compiled);

    public string Realm { get; init; } = "";

    public string Issuer { get; init; } = "";

    public string PathBase { get; init; } = "";

    public string DbSchema { get; init; } = "auth";

    public string CookieName { get; init; } = "kinxter-auth";

    public AuthMfaPolicy MfaPolicy { get; init; } = AuthMfaPolicy.OptionalStepUp;

    public bool SignupEnabled { get; init; } = true;

    public string EncryptionKey { get; init; } = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";

    public string[] AllowedOrigins { get; init; } = [];

    public AuthClientOptions[] Clients { get; init; } = [];

    public AuthExternalProvidersOptions ExternalProviders { get; init; } = new();

    public SeedAdminOptions SeedAdmin { get; init; } = new();

    public bool RequiresMfa => MfaPolicy == AuthMfaPolicy.Required;

    public string IdentityProvider => KinxterAuthIdentityProvider.ForRealm(Realm);

    public static AuthOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return FromSection(configuration.GetSection("Auth"));
    }

    public static AuthOptions FromSection(IConfiguration section, string? realmFallback = null)
    {
        ArgumentNullException.ThrowIfNull(section);

        var realm = GetRealm(section, realmFallback);
        var pathBase = GetPathBase(section, realm);
        var dbSchema = GetString(section, "DbSchema", $"auth_{realm}");

        if (!IsSafeSchemaName(dbSchema))
        {
            throw new InvalidOperationException($"Auth DB schema '{dbSchema}' is not a safe PostgreSQL schema name.");
        }

        return new AuthOptions
        {
            Realm = realm,
            Issuer = GetString(section, "Issuer", $"http://localhost:8081{pathBase}").TrimEnd('/'),
            PathBase = pathBase,
            DbSchema = dbSchema,
            CookieName = GetString(section, "CookieName", $"kinxter-auth-{realm}"),
            MfaPolicy = GetMfaPolicy(section),
            SignupEnabled = GetBool(section, "SignupEnabled", true),
            EncryptionKey = GetString(section, "EncryptionKey", "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY="),
            AllowedOrigins = section.GetSection("AllowedOrigins").Get<string[]>() ?? [],
            Clients = GetClients(section),
            ExternalProviders = GetExternalProviders(section.GetSection("ExternalProviders"), realm),
            SeedAdmin = GetSeedAdmin(section.GetSection("SeedAdmin"))
        };
    }

    public static bool IsSafeSchemaName(string dbSchema)
    {
        return SchemaNamePattern.IsMatch(dbSchema);
    }

    private static string GetRealm(IConfiguration section, string? realmFallback)
    {
        var realm = GetString(section, "Realm", realmFallback ?? "").ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(realm))
        {
            throw new InvalidOperationException("Each auth realm configuration must define a non-empty 'Realm' value.");
        }

        if (!RealmNamePattern.IsMatch(realm))
        {
            throw new InvalidOperationException($"Auth realm '{realm}' is not a safe realm name.");
        }

        return realm;
    }

    private static AuthExternalProvidersOptions GetExternalProviders(IConfiguration section, string realm)
    {
        return new AuthExternalProvidersOptions
        {
            Google = GetExternalProvider(
                section.GetSection(AuthExternalProviderNames.Google),
                AuthExternalProviderNames.Google,
                "Google",
                realm),
            Apple = GetAppleExternalProvider(section.GetSection(AuthExternalProviderNames.Apple), realm)
        };
    }

    private static AuthExternalProviderOptions GetExternalProvider(
        IConfiguration section,
        string provider,
        string displayName,
        string realm)
    {
        return new AuthExternalProviderOptions
        {
            Provider = provider,
            AuthenticationScheme = BuildExternalAuthenticationScheme(realm, provider),
            CallbackPath = BuildExternalCallbackPath(realm, provider),
            DisplayName = GetString(section, "DisplayName", displayName),
            Enabled = GetBool(section, "Enabled", false),
            ClientId = GetString(section, "ClientId", ""),
            ClientSecret = GetString(section, "ClientSecret", "")
        };
    }

    private static AuthAppleExternalProviderOptions GetAppleExternalProvider(IConfiguration section, string realm)
    {
        return new AuthAppleExternalProviderOptions
        {
            Provider = AuthExternalProviderNames.Apple,
            AuthenticationScheme = BuildExternalAuthenticationScheme(realm, AuthExternalProviderNames.Apple),
            CallbackPath = BuildExternalCallbackPath(realm, AuthExternalProviderNames.Apple),
            DisplayName = GetString(section, "DisplayName", "Apple"),
            Enabled = GetBool(section, "Enabled", false),
            ClientId = GetString(section, "ClientId", ""),
            TeamId = GetString(section, "TeamId", ""),
            KeyId = GetString(section, "KeyId", ""),
            PrivateKeyPem = GetString(section, "PrivateKeyPem", "")
        };
    }

    private static string BuildExternalAuthenticationScheme(string realm, string provider)
    {
        return $"Kinxter.{realm}.{provider}";
    }

    private static string BuildExternalCallbackPath(string realm, string provider)
    {
        return $"/signin-{realm}-{provider.ToLowerInvariant()}";
    }

    private static AuthClientOptions[] GetClients(IConfiguration section)
    {
        var clients = section.GetSection("Clients").Get<AuthClientOptions[]>();

        if (clients is { Length: > 0 })
        {
            return clients;
        }

        return [];
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
        var value = GetString(section, "PathBase", $"/realms/{realm}");

        return value.StartsWith("/", StringComparison.Ordinal)
            ? value.TrimEnd('/')
            : $"/{value.TrimEnd('/')}";
    }

    private static AuthMfaPolicy GetMfaPolicy(IConfiguration section)
    {
        var value = section["MfaPolicy"];

        return Enum.TryParse<AuthMfaPolicy>(value, ignoreCase: true, out var policy)
            ? policy
            : AuthMfaPolicy.OptionalStepUp;
    }

    public static string GetString(IConfiguration section, string key, string fallback)
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

public enum AuthMfaPolicy
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

internal sealed class AuthExternalProvidersOptions
{
    public AuthExternalProviderOptions Google { get; init; } = new()
    {
        Provider = AuthExternalProviderNames.Google,
        DisplayName = "Google"
    };

    public AuthAppleExternalProviderOptions Apple { get; init; } = new()
    {
        Provider = AuthExternalProviderNames.Apple,
        DisplayName = "Apple"
    };

    public IEnumerable<AuthExternalProviderOptions> EnabledProviders =>
        AllProviders.Where(provider => provider.Enabled);

    public IEnumerable<AuthExternalProviderOptions> ConfiguredProviders =>
        AllProviders.Where(provider => provider.Enabled && provider.IsConfigured);

    public AuthExternalProviderOptions? Find(string provider)
    {
        return AllProviders.SingleOrDefault(current =>
            string.Equals(current.Provider, provider, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<AuthExternalProviderOptions> AllProviders
    {
        get
        {
            yield return Google;
            yield return Apple;
        }
    }
}

internal class AuthExternalProviderOptions
{
    private string authenticationScheme = "";
    private string callbackPath = "";

    public string Provider { get; init; } = "";

    public string AuthenticationScheme
    {
        get => string.IsNullOrWhiteSpace(this.authenticationScheme)
            ? Provider
            : this.authenticationScheme;
        init => this.authenticationScheme = value;
    }

    public string CallbackPath
    {
        get => string.IsNullOrWhiteSpace(this.callbackPath)
            ? $"/signin-{Provider.ToLowerInvariant()}"
            : this.callbackPath;
        init => this.callbackPath = value;
    }

    public string DisplayName { get; init; } = "";

    public bool Enabled { get; init; }

    public string ClientId { get; init; } = "";

    public string ClientSecret { get; init; } = "";

    public virtual bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}

internal sealed class AuthAppleExternalProviderOptions : AuthExternalProviderOptions
{
    public string TeamId { get; init; } = "";

    public string KeyId { get; init; } = "";

    public string PrivateKeyPem { get; init; } = "";

    public override bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(TeamId) &&
        !string.IsNullOrWhiteSpace(KeyId) &&
        !string.IsNullOrWhiteSpace(PrivateKeyPem);
}

internal static class AuthExternalProviderNames
{
    public const string Google = "Google";

    public const string Apple = "Apple";
}

internal sealed class SeedAdminOptions
{
    public bool Enabled { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }
}
