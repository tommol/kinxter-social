using Kinxter.Profiles.Contracts;
using Kinxter.Profiles.Infrastructure.Persistence;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Profiles.Application;

internal sealed class ProfilesService : IProfilesService
{
    private readonly ProfilesDbContext dbContext;
    private readonly IClock clock;

    public ProfilesService(ProfilesDbContext dbContext, IClock clock)
    {
        this.dbContext = dbContext;
        this.clock = clock;
    }

    public async Task<ProfileState?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var profile = await this.dbContext.Profiles.AsNoTracking()
            .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);
        return profile is null ? null : ToState(profile);
    }

    public async Task<ProfileState?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await this.dbContext.Profiles.AsNoTracking()
            .SingleOrDefaultAsync(current => current.Id == profileId, cancellationToken);
        return profile is null ? null : ToState(profile);
    }

    public async Task<IReadOnlyCollection<ProfileState>> ListPublicCompletedAsync(
        CancellationToken cancellationToken = default) =>
        await this.dbContext.Profiles.AsNoTracking()
            .Where(profile => profile.Visibility == ProfileVisibility.Public && profile.OnboardingCompletedAt != null)
            .OrderByDescending(profile => profile.OnboardingCompletedAt)
            .Select(profile => new ProfileState(
                profile.Id,
                profile.AccountId,
                profile.Handle,
                profile.DisplayName,
                profile.Bio,
                profile.AvatarAssetId,
                profile.Visibility,
                profile.OnboardingCompletedAt))
            .ToArrayAsync(cancellationToken);

    public async Task<ProfileState?> SetVisibilityAsync(
        Guid accountId,
        ProfileVisibility visibility,
        CancellationToken cancellationToken = default)
    {
        var profile = await this.dbContext.Profiles
            .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.SetVisibility(visibility, this.clock.UtcNow);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return ToState(profile);
    }

    public async Task<bool> MarkOnboardingCompletedAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var profile = await this.dbContext.Profiles
            .SingleOrDefaultAsync(current => current.AccountId == accountId, cancellationToken);

        if (profile is null || profile.Visibility is null)
        {
            return false;
        }

        profile.MarkOnboardingCompleted(this.clock.UtcNow);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProfileState ToState(Profile profile) => new(
        profile.Id,
        profile.AccountId,
        profile.Handle,
        profile.DisplayName,
        profile.Bio,
        profile.AvatarAssetId,
        profile.Visibility,
        profile.OnboardingCompletedAt);
}
