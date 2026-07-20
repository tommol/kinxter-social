using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Events;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Application.IdentityEvents;

internal sealed class IdentityEmailConfirmedHandler : IModuleEventHandler<IdentityEmailConfirmedV1>
{
    private readonly AccountsDbContext dbContext;

    public IdentityEmailConfirmedHandler(AccountsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    public async Task HandleAsync(IdentityEmailConfirmedV1 moduleEvent, CancellationToken cancellationToken = default)
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

        account.MarkEmailAsVerified(moduleEvent.OccurredAt);

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
