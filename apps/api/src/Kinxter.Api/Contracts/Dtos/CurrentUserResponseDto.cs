namespace Kinxter.Api.Contracts.Dtos;

public sealed record CurrentUserResponseDto(
    string Subject,
    string Email,
    bool EmailVerified,
    Guid? AccountId,
    string? AccountStatus,
    string? Handle,
    string? DisplayName,
    bool OnboardingRequired);
