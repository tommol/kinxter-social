namespace Kinxter.Recommendations.Contracts;

public sealed record RecommendedProfile(Guid ProfileId, string Handle, string DisplayName, Guid? AvatarAssetId, IReadOnlyCollection<Guid> SharedTagIds, string? DistanceBand);
public sealed record RecommendedCommunity(Guid CommunityId, string Slug, string Name, int MemberCount, IReadOnlyCollection<Guid> SharedTagIds, string? DistanceBand);
public sealed record OnboardingRecommendations(IReadOnlyCollection<RecommendedProfile> Profiles, IReadOnlyCollection<RecommendedCommunity> Communities, string? EmptyReason);

public interface IRecommendationsService
{
    Task<OnboardingRecommendations> GetOnboardingAsync(Guid profileId, int limit, CancellationToken cancellationToken = default);
}
