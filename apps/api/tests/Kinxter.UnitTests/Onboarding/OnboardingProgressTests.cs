using Kinxter.Onboarding.Model;
using Xunit;

namespace Kinxter.UnitTests.Onboarding;

public sealed class OnboardingProgressTests
{
    [Fact]
    public void Complete_requires_explicit_decision_for_both_optional_steps()
    {
        var now = DateTimeOffset.UtcNow;
        var progress = new OnboardingProgress(Guid.CreateVersion7(now), now);

        Assert.Throws<InvalidOperationException>(() => progress.Complete(now));

        progress.SetStep("interests", OptionalStepStatus.Skipped, now);
        progress.SetStep("recommendations", OptionalStepStatus.Completed, now);
        progress.Complete(now);

        Assert.Equal(OptionalStepStatus.Skipped, progress.InterestsStatus);
        Assert.Equal(OptionalStepStatus.Completed, progress.RecommendationsStatus);
        Assert.Equal(now, progress.CompletedAt);
    }

    [Fact]
    public void SetStep_rejects_pending_and_unknown_steps()
    {
        var now = DateTimeOffset.UtcNow;
        var progress = new OnboardingProgress(Guid.CreateVersion7(now), now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            progress.SetStep("interests", OptionalStepStatus.Pending, now));
        Assert.Throws<ArgumentException>(() =>
            progress.SetStep("unknown", OptionalStepStatus.Completed, now));
    }
}
