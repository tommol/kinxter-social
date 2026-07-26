namespace Kinxter.Auth.Email;

internal sealed class AuthEmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 1025;

    public bool UseTls { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string FromAddress { get; init; } = "no-reply@kinxter.local";

    public string FromName { get; init; } = "Kinxter";

    public static AuthEmailOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<AuthEmailOptions>() ?? new AuthEmailOptions();

        if (string.IsNullOrWhiteSpace(options.Host) ||
            options.Port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException("Email SMTP configuration is invalid.");
        }

        return options;
    }
}
