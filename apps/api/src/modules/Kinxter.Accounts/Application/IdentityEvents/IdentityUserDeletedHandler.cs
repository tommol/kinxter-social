using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Events;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Application.IdentityEvents;

internal sealed class IdentityUserDeletedHandler : IModuleEventHandler<IdentityUserDeletedV1>
{
    private readonly AccountsDbContext dbContext;

    public IdentityUserDeletedHandler(AccountsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    public async Task HandleAsync(IdentityUserDeletedV1 moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        var account = await this.dbContext.Accounts
            .SingleOrDefaultAsync(current =>
                current.IdentityProvider == KinxterAuthIdentityProvider.ForRealm(moduleEvent.Realm) &&
                current.IdentitySubject == moduleEvent.Subject,
                cancellationToken);

        if (account is null)
        {
            return;
        }

        account.Delete(moduleEvent.OccurredAt);

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
