namespace Kinxter.Onboarding.Contracts;

public sealed record OnboardingState(
    string Status,
    string CurrentStep,
    bool ConsentsCompleted,
    bool ProfileCompleted,
    string InterestsStatus,
    string RecommendationsStatus,
    bool VisibilityCompleted,
    DateTimeOffset? CompletedAt);

public interface IOnboardingService
{
    Task<OnboardingState> GetAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<bool> SetOptionalStepAsync(Guid accountId, string step, bool skipped, CancellationToken cancellationToken = default);
    Task<bool> CompleteAsync(Guid accountId, CancellationToken cancellationToken = default);
}
