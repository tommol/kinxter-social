using Kinxter.Profiles.Contracts;
using Kinxter.Profiles.Model;
using Kinxter.Shared.Abstractions.Time;
using Kinxter.SocialGraph.Contracts;
using Kinxter.SocialGraph.Infrastructure;
using Kinxter.SocialGraph.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.SocialGraph.Application;

internal sealed class SocialGraphService(SocialGraphDbContext dbContext, IProfilesService profiles, IClock clock) : ISocialGraphService
{
    public async Task<FollowStatus> FollowAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default)
    {
        var target = await profiles.GetByIdAsync(followedId, cancellationToken) ?? throw new KeyNotFoundException("Profile does not exist.");
        var status = target.Visibility == ProfileVisibility.Public ? FollowStatus.Accepted : FollowStatus.Pending;
        var follow = await dbContext.Follows.SingleOrDefaultAsync(current => current.FollowerProfileId == followerId && current.FollowedProfileId == followedId, cancellationToken);
        if (follow is null) dbContext.Follows.Add(new Follow(followerId, followedId, status, clock.UtcNow)); else follow.Request(status, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken); return status;
    }
    public Task UnfollowAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default) => dbContext.Follows.Where(current => current.FollowerProfileId == followerId && current.FollowedProfileId == followedId).ExecuteDeleteAsync(cancellationToken);
    public async Task<IReadOnlySet<Guid>> GetAcceptedFollowedIdsAsync(Guid followerId, CancellationToken cancellationToken = default) => (await dbContext.Follows.AsNoTracking().Where(f => f.FollowerProfileId == followerId && f.Status == FollowStatus.Accepted).Select(f => f.FollowedProfileId).ToArrayAsync(cancellationToken)).ToHashSet();
    public Task<bool> IsAcceptedAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default) => dbContext.Follows.AsNoTracking().AnyAsync(f => f.FollowerProfileId == followerId && f.FollowedProfileId == followedId && f.Status == FollowStatus.Accepted, cancellationToken);
    public Task AcceptAllPendingAsync(Guid followedId, CancellationToken cancellationToken = default) => dbContext.Follows.Where(f => f.FollowedProfileId == followedId && f.Status == FollowStatus.Pending).ExecuteUpdateAsync(setters => setters.SetProperty(f => f.Status, FollowStatus.Accepted).SetProperty(f => f.AcceptedAt, clock.UtcNow).SetProperty(f => f.UpdatedAt, clock.UtcNow), cancellationToken);
}
