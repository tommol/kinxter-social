namespace Kinxter.Profiles.Contracts.Dtos;

public sealed record CreateCurrentProfileRequestDto(
    string Handle,
    string DisplayName);
