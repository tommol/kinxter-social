using Kinxter.Communities.Contracts;
using Kinxter.Communities.Infrastructure;
using Kinxter.Communities.Model;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Communities.Application;

internal sealed class CommunitiesService(CommunitiesDbContext dbContext, IClock clock) : ICommunitiesService
{
    public async Task<IReadOnlyCollection<CommunityState>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Communities.AsNoTracking().Where(community => community.Status == CommunityStatus.Published)
            .Select(community => new CommunityState(community.Id, community.OwnerProfileId, community.Slug, community.Name, community.Description, community.Status, dbContext.Memberships.Count(member => member.CommunityId == community.Id), community.PublishedAt))
            .ToArrayAsync(cancellationToken);
    public async Task<IReadOnlySet<Guid>> GetCommunityIdsForMemberAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        (await dbContext.Memberships.AsNoTracking().Where(member => member.ProfileId == profileId).Select(member => member.CommunityId).ToArrayAsync(cancellationToken)).ToHashSet();
    public async Task<bool> JoinAsync(Guid communityId, Guid profileId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Communities.AnyAsync(community => community.Id == communityId && community.Status == CommunityStatus.Published, cancellationToken)) return false;
        if (!await dbContext.Memberships.AnyAsync(member => member.CommunityId == communityId && member.ProfileId == profileId, cancellationToken))
        {
            dbContext.Memberships.Add(new CommunityMembership(communityId, profileId, false, clock.UtcNow)); await dbContext.SaveChangesAsync(cancellationToken);
        }
        return true;
    }
}
