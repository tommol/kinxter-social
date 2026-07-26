using Kinxter.Communities.Model;

namespace Kinxter.Communities.Contracts;

public sealed record CommunityState(Guid Id, Guid OwnerProfileId, string Slug, string Name, string Description, CommunityStatus Status, int MemberCount, DateTimeOffset? PublishedAt);

public interface ICommunitiesService
{
    Task<IReadOnlyCollection<CommunityState>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetCommunityIdsForMemberAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<bool> JoinAsync(Guid communityId, Guid profileId, CancellationToken cancellationToken = default);
}
