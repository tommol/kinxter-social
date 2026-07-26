namespace Kinxter.Profiles.Contracts.Dtos;

public sealed record UpdateCurrentProfileRequestDto(
    string Handle,
    string DisplayName,
    string? Bio,
    Guid? AvatarAssetId);

public sealed record SetProfileVisibilityRequestDto(string Visibility);
