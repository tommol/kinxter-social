using Kinxter.Accounts.Contracts;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.IntegrationEvents.Identity;
using Microsoft.EntityFrameworkCore;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Accounts.Application;

internal sealed class AccountsService : IAccountsService
{
    private readonly AccountsDbContext dbContext;
    private readonly AccountConsentOptions consentOptions;
    private readonly IClock clock;

    public AccountsService(AccountsDbContext dbContext, AccountConsentOptions consentOptions, IClock clock)
    {
        this.dbContext = dbContext;
        this.consentOptions = consentOptions;
        this.clock = clock;
    }

    public async Task<AccountState?> GetByIdentityAsync(
        string realm,
        string subject,
        CancellationToken cancellationToken = default)
    {
        var provider = KinxterAuthIdentityProvider.ForRealm(realm);
        return await this.dbContext.Accounts.AsNoTracking()
            .Where(account => account.IdentityProvider == provider && account.IdentitySubject == subject)
            .Select(account => new AccountState(account.Id, account.Status, account.EmailVerifiedAt != null))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasCurrentConsentsAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        this.dbContext.AccountConsents.AsNoTracking().AnyAsync(consent =>
            consent.AccountId == accountId &&
            consent.AdultConfirmed &&
            consent.TermsVersion == this.consentOptions.TermsVersion &&
            consent.PrivacyVersion == this.consentOptions.PrivacyVersion,
            cancellationToken);

    public Task<bool> IsActiveAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        this.dbContext.Accounts.AsNoTracking().AnyAsync(
            account => account.Id == accountId && account.Status == Model.AccountStatus.Active,
            cancellationToken);

    public async Task<AcceptConsentsStatus> AcceptConsentsAsync(
        Guid accountId,
        bool adultConfirmed,
        string termsVersion,
        string privacyVersion,
        string locale,
        CancellationToken cancellationToken = default)
    {
        if (!adultConfirmed) return AcceptConsentsStatus.AdultConfirmationRequired;
        if (termsVersion != this.consentOptions.TermsVersion || privacyVersion != this.consentOptions.PrivacyVersion) return AcceptConsentsStatus.StaleDocumentVersion;
        if (!await this.dbContext.Accounts.AnyAsync(account => account.Id == accountId && account.Status == Model.AccountStatus.Active, cancellationToken)) return AcceptConsentsStatus.AccountNotActive;
        if (!await HasCurrentConsentsAsync(accountId, cancellationToken))
        {
            this.dbContext.AccountConsents.Add(new Model.AccountConsent(Guid.CreateVersion7(this.clock.UtcNow), accountId, termsVersion, privacyVersion, locale, this.clock.UtcNow));
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
        return AcceptConsentsStatus.Accepted;
    }
}
