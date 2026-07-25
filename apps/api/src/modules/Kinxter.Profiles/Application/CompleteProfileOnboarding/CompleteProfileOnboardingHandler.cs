using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Application;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Profiles.Application.CompleteProfileOnboarding;

internal sealed class CompleteProfileOnboardingHandler
    : ICommandHandler<CompleteProfileOnboardingCommand, CompleteProfileOnboardingResult>
{
    private readonly AccountsDbContext accountsDbContext;
    private readonly ProfilesDbContext profilesDbContext;
    private readonly IClock clock;

    public CompleteProfileOnboardingHandler(
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

    public async Task<CompleteProfileOnboardingResult> HandleAsync(
        CompleteProfileOnboardingCommand command,
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
            return CompleteProfileOnboardingResult.Failure(
                CompleteProfileOnboardingStatus.AccountNotInitialized);
        }

        var profile = await this.profilesDbContext.Profiles
            .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);

        if (profile is null)
        {
            return CompleteProfileOnboardingResult.Failure(
                CompleteProfileOnboardingStatus.ProfileNotCreated);
        }

        profile.CompleteOnboarding(
            command.Bio,
            command.ProfilePictureUrl,
            this.clock.UtcNow);

        await this.profilesDbContext.SaveChangesAsync(cancellationToken);

        return CompleteProfileOnboardingResult.Success(profile);
    }
}
