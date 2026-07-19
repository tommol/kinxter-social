using Kinxter.Accounts.Contracts.Events;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Events;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Profiles.Application.CreateProfileOnAccountCreated;

internal sealed class CreateProfileOnAccountCreatedHandler : IModuleEventHandler<AccountCreated>
{
    private readonly ProfilesDbContext dbContext;

    public CreateProfileOnAccountCreatedHandler(ProfilesDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    public async Task HandleAsync(AccountCreated moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        var profileExists = await this.dbContext.Profiles
            .AnyAsync(profile => profile.AccountId == moduleEvent.AccountId, cancellationToken);

        if (profileExists)
        {
            return;
        }

        var profile = Profile.Create(
            Guid.CreateVersion7(moduleEvent.OccurredAt),
            moduleEvent.AccountId,
            moduleEvent.Handle,
            moduleEvent.DisplayName,
            moduleEvent.OccurredAt);

        this.dbContext.Profiles.Add(profile);

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
