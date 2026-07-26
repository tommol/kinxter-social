namespace Kinxter.Profiles.Contracts;

public interface IProfileAccessEvaluator
{
    Task<bool> CanViewDetailsAsync(Guid viewerProfileId, Guid targetProfileId, CancellationToken cancellationToken = default);
}
