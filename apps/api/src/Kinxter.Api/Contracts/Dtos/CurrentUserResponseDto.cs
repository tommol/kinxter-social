namespace Kinxter.Api.Contracts.Dtos;

public sealed record CurrentUserResponseDto(
    string Subject,
    string Email,
    bool EmailVerified,
    Guid? AccountId,
    string? AccountStatus,
    Guid? ProfileId,
    string? Handle,
    string? DisplayName,
    string? Bio,
    string? ProfilePictureUrl,
    Guid? AvatarAssetId,
    string? ProfileVisibility,
    bool AccountRequired,
    bool ProfileRequired,
    bool OnboardingRequired,
    string OnboardingStatus,
    string OnboardingCurrentStep,
    string InterestsStepStatus,
    string RecommendationsStepStatus,
    bool ConsentsRequired,
    bool VisibilityRequired);
