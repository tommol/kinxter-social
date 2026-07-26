using Microsoft.Extensions.Configuration;

namespace Kinxter.Accounts;

public sealed class AccountConsentOptions
{
    public const string SectionName = "Legal";

    public string TermsVersion { get; init; } = "2026-07-26";

    public string PrivacyVersion { get; init; } = "2026-07-26";

    public static AccountConsentOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<AccountConsentOptions>() ?? new();

        if (string.IsNullOrWhiteSpace(options.TermsVersion) || string.IsNullOrWhiteSpace(options.PrivacyVersion))
        {
            throw new InvalidOperationException("Current legal document versions must be configured.");
        }

        return options;
    }
}
