using Kinxter.Profiles.Model;

namespace Kinxter.Profiles.Contracts.Dtos;

public sealed record ProfileResponseDto(
    Guid ProfileId,
    Guid AccountId,
    string Handle,
    string DisplayName,
    string? Bio,
    string? ProfilePictureUrl,
    Guid? AvatarAssetId,
    string? Visibility,
    DateTimeOffset? OnboardingCompletedAt)
{
    public static ProfileResponseDto From(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new ProfileResponseDto(
            profile.Id,
            profile.AccountId,
            profile.Handle,
            profile.DisplayName,
            profile.Bio,
            profile.ProfilePictureUrl,
            profile.AvatarAssetId,
            profile.Visibility?.ToString(),
            profile.OnboardingCompletedAt);
    }
}
