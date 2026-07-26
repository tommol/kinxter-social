using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Accounts.Model;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Events;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Application.IdentityEvents;

internal sealed class IdentityUserDisabledHandler : IModuleEventHandler<IdentityUserDisabledV1>
{
    private readonly AccountsDbContext dbContext;

    public IdentityUserDisabledHandler(AccountsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    public async Task HandleAsync(IdentityUserDisabledV1 moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        if (await this.dbContext.InboxMessages.AnyAsync(message => message.EventId == moduleEvent.EventId, cancellationToken))
        {
            return;
        }

        var account = await this.dbContext.Accounts
            .SingleOrDefaultAsync(current =>
                current.IdentityProvider == KinxterAuthIdentityProvider.ForRealm(moduleEvent.Realm) &&
                current.IdentitySubject == moduleEvent.Subject,
                cancellationToken);

        if (account is not null)
        {
            account.Disable(moduleEvent.OccurredAt);
        }

        this.dbContext.InboxMessages.Add(new ProcessedAccountEvent(
            moduleEvent.EventId,
            nameof(IdentityUserDisabledV1),
            moduleEvent.OccurredAt));
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
