using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Application;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;
using Kinxter.Media.Contracts;

namespace Kinxter.Profiles.Application.CreateCurrentProfile;

internal sealed class CreateCurrentProfileHandler
    : ICommandHandler<CreateCurrentProfileCommand, CreateCurrentProfileResult>
{
    private readonly AccountsDbContext accountsDbContext;
    private readonly ProfilesDbContext profilesDbContext;
    private readonly IClock clock;
    private readonly IMediaService mediaService;

    public CreateCurrentProfileHandler(
        AccountsDbContext accountsDbContext,
        ProfilesDbContext profilesDbContext,
        IClock clock,
        IMediaService mediaService)
    {
        ArgumentNullException.ThrowIfNull(accountsDbContext);
        ArgumentNullException.ThrowIfNull(profilesDbContext);
        ArgumentNullException.ThrowIfNull(clock);

        this.accountsDbContext = accountsDbContext;
        this.profilesDbContext = profilesDbContext;
        this.clock = clock;
        this.mediaService = mediaService;
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
            .Where(account => account.Status == Kinxter.Accounts.Model.AccountStatus.Active)
            .Select(account => account.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (accountId == Guid.Empty)
        {
            return CreateCurrentProfileResult.Failure(CreateCurrentProfileStatus.AccountNotInitialized);
        }

        if (command.AvatarAssetId is Guid avatarAssetId &&
            !await this.mediaService.IsReadyAndOwnedAsync(accountId, avatarAssetId, cancellationToken))
        {
            return CreateCurrentProfileResult.Failure(CreateCurrentProfileStatus.AvatarAssetNotReady);
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
        profile.UpdateDetails(command.Handle, command.DisplayName, command.Bio, command.AvatarAssetId, now);

        this.profilesDbContext.Profiles.Add(profile);
        try
        {
            await this.profilesDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            this.profilesDbContext.ChangeTracker.Clear();
            var concurrentlyCreated = await this.profilesDbContext.Profiles
                .AsNoTracking()
                .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);
            return concurrentlyCreated is null
                ? CreateCurrentProfileResult.Failure(CreateCurrentProfileStatus.HandleAlreadyTaken)
                : CreateCurrentProfileResult.Success(CreateCurrentProfileStatus.AlreadyCreated, concurrentlyCreated);
        }

        return CreateCurrentProfileResult.Success(CreateCurrentProfileStatus.Created, profile);
    }
}
