namespace Kinxter.Accounts.Model;

public sealed class AccountConsent
{
    private AccountConsent()
    {
        TermsVersion = null!;
        PrivacyVersion = null!;
        Locale = null!;
    }

    public AccountConsent(
        Guid id,
        Guid accountId,
        string termsVersion,
        string privacyVersion,
        string locale,
        DateTimeOffset acceptedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(termsVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(privacyVersion);

        Id = id;
        AccountId = accountId;
        AdultConfirmed = true;
        TermsVersion = termsVersion.Trim();
        PrivacyVersion = privacyVersion.Trim();
        Locale = locale is "pl" ? "pl" : "en";
        AcceptedAt = acceptedAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public bool AdultConfirmed { get; private set; }

    public string TermsVersion { get; private set; }

    public string PrivacyVersion { get; private set; }

    public string Locale { get; private set; }

    public DateTimeOffset AcceptedAt { get; private set; }
}
