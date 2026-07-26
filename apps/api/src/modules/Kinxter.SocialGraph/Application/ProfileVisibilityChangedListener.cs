using Kinxter.Profiles.Contracts;
using Kinxter.Profiles.Model;
using Kinxter.SocialGraph.Contracts;

namespace Kinxter.SocialGraph.Application;

internal sealed class ProfileVisibilityChangedListener(ISocialGraphService graph) : IProfileVisibilityChangedListener
{
    public Task OnChangedAsync(Guid profileId, ProfileVisibility visibility, CancellationToken cancellationToken = default) =>
        visibility == ProfileVisibility.Public
            ? graph.AcceptAllPendingAsync(profileId, cancellationToken)
            : Task.CompletedTask;
}
