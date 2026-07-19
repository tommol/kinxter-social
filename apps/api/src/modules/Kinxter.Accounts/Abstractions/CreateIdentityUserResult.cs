namespace Kinxter.Accounts.Abstractions;

public sealed record CreateIdentityUserResult(
    string Provider,
    string Subject,
    string Email,
    bool EmailVerified);
