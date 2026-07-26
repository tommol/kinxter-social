using Kinxter.SocialGraph.Model;

namespace Kinxter.SocialGraph.Contracts;

public interface ISocialGraphService
{
    Task<FollowStatus> FollowAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
    Task UnfollowAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetAcceptedFollowedIdsAsync(Guid followerId, CancellationToken cancellationToken = default);
    Task<bool> IsAcceptedAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
    Task AcceptAllPendingAsync(Guid followedId, CancellationToken cancellationToken = default);
}
