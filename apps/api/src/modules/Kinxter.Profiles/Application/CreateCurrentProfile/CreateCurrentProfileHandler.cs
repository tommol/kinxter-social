using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Application;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Profiles.Application.CreateCurrentProfile;

internal sealed class CreateCurrentProfileHandler
    : ICommandHandler<CreateCurrentProfileCommand, CreateCurrentProfileResult>
{
    private readonly AccountsDbContext accountsDbContext;
    private readonly ProfilesDbContext profilesDbContext;
    private readonly IClock clock;

    public CreateCurrentProfileHandler(
        AccountsDbContext accountsDbContext,
        ProfilesDbContext profilesDbContext,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(accountsDbContext);
        ArgumentNullException.ThrowIfNull(profilesDbContext);
        ArgumentNullException.ThrowIfNull(clock);

        this.accountsDbContext = accountsDbContext;
        this.profilesDbContext = profilesDbContext;
        this.clock = clock;
    }

    public async Task<CreateCurrentProfileResult> HandleAsync(
        CreateCurrentProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var accountId = await this.accountsDbContext.Accounts
            .AsNoTracking()
            .Where(account =>
                account.IdentityProvider == command.IdentityProvider &&
                account.IdentitySubject == command.IdentitySubject)
            .Select(account => account.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (accountId == Guid.Empty)
        {
            return CreateCurrentProfileResult.Failure(CreateCurrentProfileStatus.AccountNotInitialized);
        }

        var existingProfile = await this.profilesDbContext.Profiles
            .SingleOrDefaultAsync(profile => profile.AccountId == accountId, cancellationToken);

        if (existingProfile is not null)
        {
            return CreateCurrentProfileResult.Success(
                CreateCurrentProfileStatus.AlreadyCreated,
                existingProfile);
        }

        var normalizedHandle = Profile.NormalizeHandle(command.Handle);
        var handleExists = await this.profilesDbContext.Profiles
            .AsNoTracking()
            .AnyAsync(profile => profile.NormalizedHandle == normalizedHandle, cancellationToken);

        if (handleExists)
        {
            return CreateCurrentProfileResult.Failure(CreateCurrentProfileStatus.HandleAlreadyTaken);
        }

        var now = this.clock.UtcNow;
        var profile = Profile.Create(
            Guid.CreateVersion7(now),
            accountId,
            command.Handle,
            command.DisplayName,
            now);

        this.profilesDbContext.Profiles.Add(profile);
        await this.profilesDbContext.SaveChangesAsync(cancellationToken);

        return CreateCurrentProfileResult.Success(CreateCurrentProfileStatus.Created, profile);
    }
}
