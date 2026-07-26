using Kinxter.Profiles.Model;

namespace Kinxter.Profiles.Contracts;

public sealed record ProfileState(
    Guid ProfileId,
    Guid AccountId,
    string Handle,
    string DisplayName,
    string? Bio,
    Guid? AvatarAssetId,
    ProfileVisibility? Visibility,
    DateTimeOffset? OnboardingCompletedAt);

public interface IProfilesService
{
    Task<ProfileState?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<ProfileState?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProfileState>> ListPublicCompletedAsync(CancellationToken cancellationToken = default);

    Task<ProfileState?> SetVisibilityAsync(
        Guid accountId,
        ProfileVisibility visibility,
        CancellationToken cancellationToken = default);

    Task<bool> MarkOnboardingCompletedAsync(Guid accountId, CancellationToken cancellationToken = default);
}
