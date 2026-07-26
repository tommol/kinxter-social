namespace Kinxter.Accounts.Contracts.Dtos;

public sealed record AcceptAccountConsentsRequestDto(
    bool AdultConfirmed,
    string TermsVersion,
    string PrivacyVersion,
    string Locale);

public sealed record AccountConsentsResponseDto(
    bool Accepted,
    bool AdultConfirmed,
    string CurrentTermsVersion,
    string CurrentPrivacyVersion,
    DateTimeOffset? AcceptedAt);
