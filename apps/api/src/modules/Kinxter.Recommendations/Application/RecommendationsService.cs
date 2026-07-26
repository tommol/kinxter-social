using Kinxter.Communities.Contracts;
using Kinxter.Locations.Contracts;
using Kinxter.Profiles.Contracts;
using Kinxter.Recommendations.Contracts;
using Kinxter.SocialGraph.Contracts;
using Kinxter.Tags.Contracts;

namespace Kinxter.Recommendations.Application;

internal sealed class RecommendationsService(
    IProfilesService profiles,
    ITagsService tags,
    ILocationsService locations,
    ICommunitiesService communities,
    ISocialGraphService graph) : IRecommendationsService
{
    public async Task<OnboardingRecommendations> GetOnboardingAsync(Guid profileId, int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 20);
        var selectedTags = await tags.GetTagIdsAsync("profile", profileId, cancellationToken);
        var selectedLocation = await locations.GetForEntityAsync("profile", profileId, cancellationToken);
        if (selectedTags.Count == 0 && selectedLocation is null) return new([], [], "Select at least one kinktag or a location to receive recommendations.");

        var followed = await graph.GetAcceptedFollowedIdsAsync(profileId, cancellationToken);
        var profileScores = new List<(double Score, RecommendedProfile Item)>();
        foreach (var candidate in await profiles.ListPublicCompletedAsync(cancellationToken))
        {
            if (candidate.ProfileId == profileId || followed.Contains(candidate.ProfileId)) continue;
            var candidateTags = await tags.GetTagIdsAsync("profile", candidate.ProfileId, cancellationToken);
            var candidateLocation = await locations.GetForEntityAsync("profile", candidate.ProfileId, cancellationToken);
            var ranked = await RankAsync(selectedTags, selectedLocation, candidateTags, candidateLocation, cancellationToken);
            profileScores.Add((ranked.Score, new RecommendedProfile(candidate.ProfileId, candidate.Handle, candidate.DisplayName, candidate.AvatarAssetId, ranked.SharedTags, ranked.DistanceBand)));
        }

        var memberships = await communities.GetCommunityIdsForMemberAsync(profileId, cancellationToken);
        var communityScores = new List<(double Score, RecommendedCommunity Item)>();
        foreach (var candidate in await communities.ListPublishedAsync(cancellationToken))
        {
            if (memberships.Contains(candidate.Id)) continue;
            var candidateTags = await tags.GetTagIdsAsync("community", candidate.Id, cancellationToken);
            var candidateLocation = await locations.GetForEntityAsync("community", candidate.Id, cancellationToken);
            var ranked = await RankAsync(selectedTags, selectedLocation, candidateTags, candidateLocation, cancellationToken);
            communityScores.Add((ranked.Score, new RecommendedCommunity(candidate.Id, candidate.Slug, candidate.Name, candidate.MemberCount, ranked.SharedTags, ranked.DistanceBand)));
        }

        return new(
            profileScores.OrderByDescending(item => item.Score).ThenBy(item => item.Item.ProfileId).Take(limit).Select(item => item.Item).ToArray(),
            communityScores.OrderByDescending(item => item.Score).ThenByDescending(item => item.Item.MemberCount).Take(limit).Select(item => item.Item).ToArray(),
            null);
    }

    private async Task<RankResult> RankAsync(IReadOnlySet<Guid> selectedTags, PlaceState? selectedLocation, IReadOnlySet<Guid> candidateTags, PlaceState? candidateLocation, CancellationToken cancellationToken)
    {
        var shared = selectedTags.Intersect(candidateTags).ToArray();
        var unionCount = selectedTags.Union(candidateTags).Count();
        var hasTags = selectedTags.Count > 0;
        var tagScore = unionCount == 0 ? 0 : (double)shared.Length / unionCount;
        var hasLocation = selectedLocation is not null && candidateLocation is not null;
        var distance = hasLocation
            ? await locations.GetDistanceKmAsync(selectedLocation!.PlaceId, candidateLocation!.PlaceId, cancellationToken)
            : (double?)null;
        var locationScore = distance is null ? 0 : Math.Exp(-distance.Value / 100d);
        var score = hasTags && hasLocation ? 0.7 * tagScore + 0.3 * locationScore : hasTags ? tagScore : locationScore;
        return new(score, shared, DistanceBand(distance));
    }

    private static string? DistanceBand(double? distance) => distance switch { null => null, <= 25 => "nearby", <= 100 => "regional", <= 300 => "extended", _ => "remote" };
    private sealed record RankResult(double Score, Guid[] SharedTags, string? DistanceBand);
}
