using Kinxter.Profiles.Model;

namespace Kinxter.Profiles.Application.CompleteProfileOnboarding;

public sealed record CompleteProfileOnboardingResult(
    CompleteProfileOnboardingStatus Status,
    Profile? Profile)
{
    public static CompleteProfileOnboardingResult Success(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new CompleteProfileOnboardingResult(
            CompleteProfileOnboardingStatus.Completed,
            profile);
    }

    public static CompleteProfileOnboardingResult Failure(CompleteProfileOnboardingStatus status)
    {
        return new CompleteProfileOnboardingResult(status, null);
    }
}
