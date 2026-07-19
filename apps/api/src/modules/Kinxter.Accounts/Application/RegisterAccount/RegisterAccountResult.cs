namespace Kinxter.Accounts.Application.RegisterAccount;

public sealed record RegisterAccountResult(
    Guid AccountId,
    RegisterAccountStatus Status);
