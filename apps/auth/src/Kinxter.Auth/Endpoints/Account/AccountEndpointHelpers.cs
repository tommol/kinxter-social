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

    private static AuthExternalProviderOptions? GetConfiguredExternalProvider(
        AuthOptions options,
        string provider)
    {
        var externalProvider = options.ExternalProviders.Find(provider);

        return externalProvider is { Enabled: true, IsConfigured: true }
            ? externalProvider
            : null;
    }

    private static string BuildAccountPath(HttpContext context, string path)
    {
        return $"{context.Request.PathBase}{path}";
    }

    private static string BuildExternalCallbackPath(
        HttpContext context,
        string returnUrl,
        bool link)
    {
        var path = BuildAccountPath(context, "/account/external-callback");
        var callback = $"{path}?returnUrl={Uri.EscapeDataString(returnUrl)}";

        return link
            ? $"{callback}&link=true"
            : callback;
    }
}
