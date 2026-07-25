namespace Kinxter.Auth;

internal sealed class AuthAdminOptions
{
    public const string SectionName = "AuthAdmin";

    public bool Enabled { get; init; }

    public string PathBase { get; init; } = "/control";

    public string CookieName { get; init; } = "kinxter-auth-control";

    public int SessionHours { get; init; } = 8;

    public AuthAdminBootstrapOptions Bootstrap { get; init; } = new();

    public string LoginPath => $"{PathBase}/login";

    public static AuthAdminOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(SectionName);
        var pathBase = section["PathBase"]?.Trim().TrimEnd('/') ?? "/control";

        if (!pathBase.StartsWith("/", StringComparison.Ordinal) ||
            pathBase.Length < 2 ||
            pathBase.Contains("//", StringComparison.Ordinal) ||
            pathBase.Contains('?', StringComparison.Ordinal) ||
            pathBase.Contains('#', StringComparison.Ordinal) ||
            pathBase.Split('/').Any(segment => segment is "." or "..") ||
            IsReservedPath(pathBase))
        {
            throw new InvalidOperationException(
                $"Auth admin path base '{pathBase}' must be a safe, non-realm application path.");
        }

        var sessionHours = section.GetValue("SessionHours", 8);
        var bootstrapUsername = section["Bootstrap:Username"]?.Trim();
        var bootstrapPassword = section["Bootstrap:Password"];

        if (string.IsNullOrWhiteSpace(bootstrapUsername) != string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            throw new InvalidOperationException(
                "Auth admin bootstrap username and password must be configured together.");
        }

        if (!string.IsNullOrEmpty(bootstrapPassword) && bootstrapPassword.Length < 12)
        {
            throw new InvalidOperationException(
                "Auth admin bootstrap password must contain at least 12 characters.");
        }

        return new AuthAdminOptions
        {
            Enabled = section.GetValue("Enabled", false),
            PathBase = pathBase,
            CookieName = GetString(section, "CookieName", "kinxter-auth-control"),
            SessionHours = sessionHours is >= 1 and <= 24 ? sessionHours : 8,
            Bootstrap = new AuthAdminBootstrapOptions
            {
                Username = bootstrapUsername,
                Password = bootstrapPassword
            }
        };
    }

    private static bool IsReservedPath(string pathBase)
    {
        var reservedPaths = new[]
        {
            "/account",
            "/connect",
            "/realms",
            "/.well-known"
        };

        return reservedPaths.Any(path =>
            string.Equals(pathBase, path, StringComparison.OrdinalIgnoreCase) ||
            pathBase.StartsWith($"{path}/", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetString(IConfiguration section, string key, string fallback)
    {
        var value = section[key];

        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}

internal sealed class AuthAdminBootstrapOptions
{
    public string? Username { get; init; }

    public string? Password { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);
}
