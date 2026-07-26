namespace Kinxter.Onboarding.Model;

public enum OptionalStepStatus { Pending = 1, Completed = 2, Skipped = 3 }

public sealed class OnboardingProgress
{
    private OnboardingProgress() { }
    public OnboardingProgress(Guid accountId, DateTimeOffset startedAt) { AccountId = accountId; StartedAt = startedAt; InterestsStatus = OptionalStepStatus.Pending; RecommendationsStatus = OptionalStepStatus.Pending; }
    public Guid AccountId { get; private set; }
    public OptionalStepStatus InterestsStatus { get; private set; }
    public OptionalStepStatus RecommendationsStatus { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public void SetStep(string step, OptionalStepStatus status, DateTimeOffset at)
    {
        if (status == OptionalStepStatus.Pending) throw new ArgumentOutOfRangeException(nameof(status));
        if (step == "interests") InterestsStatus = status; else if (step == "recommendations") RecommendationsStatus = status; else throw new ArgumentException("Unknown onboarding step.", nameof(step)); UpdatedAt = at;
    }
    public void Complete(DateTimeOffset at) { if (InterestsStatus == OptionalStepStatus.Pending || RecommendationsStatus == OptionalStepStatus.Pending) throw new InvalidOperationException("Optional steps must be completed or skipped explicitly."); CompletedAt ??= at; UpdatedAt = at; }
}
