using Kinxter.Profiles.Model;

namespace Kinxter.Profiles.Contracts;

public interface IProfileVisibilityChangedListener
{
    Task OnChangedAsync(Guid profileId, ProfileVisibility visibility, CancellationToken cancellationToken = default);
}
