using Kinxter.Accounts.Contracts.Events;
using Kinxter.Accounts.Infrastructure.Outbox;
using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Accounts.Model;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Application.IdentityEvents;

internal sealed class CreateAccountOnIdentityUserRegisteredHandler : IModuleEventHandler<IdentityUserRegisteredV1>
{
    private readonly AccountsDbContext dbContext;
    private readonly IOutboxWriter<AccountsOutbox> outboxWriter;

    public CreateAccountOnIdentityUserRegisteredHandler(
        AccountsDbContext dbContext,
        IOutboxWriter<AccountsOutbox> outboxWriter)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(outboxWriter);

        this.dbContext = dbContext;
        this.outboxWriter = outboxWriter;
    }

    public async Task HandleAsync(IdentityUserRegisteredV1 moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        var identityProvider = KinxterAuthIdentityProvider.ForRealm(moduleEvent.Realm);
        var accountExists = await this.dbContext.Accounts
            .AnyAsync(account =>
                account.IdentityProvider == identityProvider &&
                account.IdentitySubject == moduleEvent.Subject,
                cancellationToken);

        if (accountExists)
        {
            return;
        }

        var account = Account.Create(
            Guid.CreateVersion7(moduleEvent.OccurredAt),
            moduleEvent.Email,
            identityProvider,
            moduleEvent.Subject,
            moduleEvent.EmailVerified,
            moduleEvent.OccurredAt);
        var accountCreated = new AccountCreated(
            Guid.CreateVersion7(moduleEvent.OccurredAt),
            moduleEvent.OccurredAt,
            account.Id);

        this.dbContext.Accounts.Add(account);
        await this.outboxWriter.AddAsync(accountCreated, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
