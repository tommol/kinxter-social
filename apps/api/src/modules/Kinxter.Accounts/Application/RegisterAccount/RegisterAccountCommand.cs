using Kinxter.Shared.Abstractions.Application;

namespace Kinxter.Accounts.Application.RegisterAccount;

public sealed class RegisterAccountCommand : ICommand<RegisterAccountResult>
{
    public RegisterAccountCommand(string email, string password, string handle, string displayName)
    {
        Email = email;
        Password = password;
        Handle = handle;
        DisplayName = displayName;
    }

    public string Email { get; }
    public string Password { get; }
    public string Handle { get; }
    public string DisplayName { get; }
}
