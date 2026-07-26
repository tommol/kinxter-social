using Kinxter.Accounts.Model;

namespace Kinxter.Accounts.Contracts;

public sealed record AccountState(Guid AccountId, AccountStatus Status, bool EmailVerified);

public interface IAccountsService
{
    Task<AccountState?> GetByIdentityAsync(
        string realm,
        string subject,
        CancellationToken cancellationToken = default);

    Task<bool> HasCurrentConsentsAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<AcceptConsentsStatus> AcceptConsentsAsync(
        Guid accountId,
        bool adultConfirmed,
        string termsVersion,
        string privacyVersion,
        string locale,
        CancellationToken cancellationToken = default);
}

public enum AcceptConsentsStatus
{
    Accepted = 1,
    AccountNotActive = 2,
    AdultConfirmationRequired = 3,
    StaleDocumentVersion = 4
}
