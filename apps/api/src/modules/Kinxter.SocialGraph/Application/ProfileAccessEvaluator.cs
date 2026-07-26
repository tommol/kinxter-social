using Kinxter.Profiles.Contracts;
using Kinxter.Profiles.Model;
using Kinxter.SocialGraph.Contracts;

namespace Kinxter.SocialGraph.Application;

internal sealed class ProfileAccessEvaluator(IProfilesService profiles, ISocialGraphService graph) : IProfileAccessEvaluator
{
    public async Task<bool> CanViewDetailsAsync(Guid viewerProfileId, Guid targetProfileId, CancellationToken cancellationToken = default)
    {
        if (viewerProfileId == targetProfileId) return true;
        var target = await profiles.GetByIdAsync(targetProfileId, cancellationToken);
        return target?.Visibility == ProfileVisibility.Public || await graph.IsAcceptedAsync(viewerProfileId, targetProfileId, cancellationToken);
    }
}
