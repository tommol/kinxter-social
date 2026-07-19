namespace Kinxter.Accounts.Abstractions;

public sealed record CreateIdentityUserRequest(
    string Email,
    string Password);
