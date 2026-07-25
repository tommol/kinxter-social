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
    bool AccountRequired,
    bool ProfileRequired,
    bool OnboardingRequired);
