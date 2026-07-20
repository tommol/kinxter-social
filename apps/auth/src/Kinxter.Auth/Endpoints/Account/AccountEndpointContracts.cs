namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private sealed record ForgotPasswordRequest(string Email);

    private sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
}
