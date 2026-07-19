using Kinxter.Accounts.Application.RegisterAccount;

namespace Kinxter.Accounts.Contracts.Dtos;

public sealed record RegisterAccountResponseDto(
    Guid AccountId,
    string Status)
{
    public static RegisterAccountResponseDto From(RegisterAccountResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new RegisterAccountResponseDto(
            result.AccountId,
            result.Status.ToString());
    }
}
