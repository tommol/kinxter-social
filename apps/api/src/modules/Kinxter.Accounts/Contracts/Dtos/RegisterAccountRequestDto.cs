namespace Kinxter.Accounts.Contracts.Dtos;

public sealed record RegisterAccountRequestDto(
    string Email,
    string Password,
    string Handle,
    string DisplayName);
