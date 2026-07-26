using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Accounts.Contracts.Events;
using Kinxter.Accounts.Infrastructure.Outbox;
using Kinxter.Accounts.Model;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Application.IdentityEvents;

internal sealed class IdentityEmailConfirmedHandler : IModuleEventHandler<IdentityEmailConfirmedV1>
{
    private readonly AccountsDbContext dbContext;
    private readonly IOutboxWriter<AccountsOutbox> outboxWriter;

    public IdentityEmailConfirmedHandler(
        AccountsDbContext dbContext,
        IOutboxWriter<AccountsOutbox> outboxWriter)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
        this.outboxWriter = outboxWriter;
    }

    public async Task HandleAsync(IdentityEmailConfirmedV1 moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        if (await this.dbContext.InboxMessages.AnyAsync(message => message.EventId == moduleEvent.EventId, cancellationToken))
        {
            return;
        }

        var identityProvider = KinxterAuthIdentityProvider.ForRealm(moduleEvent.Realm);
        var account = await this.dbContext.Accounts
            .SingleOrDefaultAsync(current =>
                current.IdentityProvider == identityProvider &&
                current.IdentitySubject == moduleEvent.Subject,
                cancellationToken);

        if (account is null)
        {
            account = Account.Create(
                Guid.CreateVersion7(moduleEvent.OccurredAt),
                moduleEvent.Email,
                identityProvider,
                moduleEvent.Subject,
                emailVerified: true,
                moduleEvent.OccurredAt);
            this.dbContext.Accounts.Add(account);
            await this.outboxWriter.AddAsync(
                new AccountCreated(Guid.CreateVersion7(moduleEvent.OccurredAt), moduleEvent.OccurredAt, account.Id),
                cancellationToken);
        }
        else
        {
            account.MarkEmailAsVerified(moduleEvent.OccurredAt);
        }

        this.dbContext.InboxMessages.Add(new ProcessedAccountEvent(
            moduleEvent.EventId,
            nameof(IdentityEmailConfirmedV1),
            moduleEvent.OccurredAt));

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
