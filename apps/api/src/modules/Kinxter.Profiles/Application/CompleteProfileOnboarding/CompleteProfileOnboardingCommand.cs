using Kinxter.Shared.Abstractions.Application;

namespace Kinxter.Profiles.Application.CompleteProfileOnboarding;

public sealed record CompleteProfileOnboardingCommand(
    string IdentityProvider,
    string IdentitySubject,
    string? Bio,
    string? ProfilePictureUrl) : ICommand<CompleteProfileOnboardingResult>;
