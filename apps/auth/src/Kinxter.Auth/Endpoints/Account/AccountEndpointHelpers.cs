using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth;

internal static partial class AccountEndpoints
{
    private static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join(" ", result.Errors.Select(error => error.Description));
    }

    private static string CleanAuthenticatorCode(string code)
    {
        return code.Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal);
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        return string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/", StringComparison.Ordinal)
            ? "/"
            : returnUrl;
    }
}
