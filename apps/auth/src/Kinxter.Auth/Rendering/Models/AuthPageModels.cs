namespace Kinxter.Auth.Rendering.Models;

internal static class AuthPagePaths
{
    public static string AccountPath(AuthOptions options, string path)
    {
        return string.IsNullOrWhiteSpace(options.PathBase)
            ? path
            : $"{options.PathBase}{path}";
    }

    public static string EscapedReturnUrl(string returnUrl)
    {
        return Uri.EscapeDataString(returnUrl);
    }
}

internal sealed record AuthRealmLinkViewModel(
    string Realm,
    string PathBase);

internal sealed class AuthServerHomeViewModel
{
    public AuthServerHomeViewModel(AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Realms = options.Realms
            .Select(realm => new AuthRealmLinkViewModel(realm.Realm, realm.PathBase))
            .ToArray();
    }

    public IReadOnlyList<AuthRealmLinkViewModel> Realms { get; }
}

internal sealed class AuthRealmHomeViewModel
{
    public AuthRealmHomeViewModel(AuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Realm = options.Realm;
        Issuer = options.Issuer;
        MfaPolicy = options.MfaPolicy.ToString();
        SignupEnabled = options.SignupEnabled;
        LoginPath = AuthPagePaths.AccountPath(options, "/account/login");
        RegisterPath = AuthPagePaths.AccountPath(options, "/account/register");
    }

    public string Realm { get; }

    public string Issuer { get; }

    public string MfaPolicy { get; }

    public bool SignupEnabled { get; }

    public string LoginPath { get; }

    public string RegisterPath { get; }
}

internal sealed record AuthExternalLoginButtonViewModel(
    string DisplayName,
    string ActionPath);

internal sealed class AuthLoginPageViewModel
{
    public AuthLoginPageViewModel(AuthOptions options, string? returnUrl, string? error = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        SignupEnabled = options.SignupEnabled;
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        Error = error;
        RegisterPath = AuthPagePaths.AccountPath(options, "/account/register");
        EscapedReturnUrl = AuthPagePaths.EscapedReturnUrl(ReturnUrl);
        ExternalProviders = options.ExternalProviders.ConfiguredProviders
            .Select(provider => new AuthExternalLoginButtonViewModel(
                provider.DisplayName,
                AuthPagePaths.AccountPath(options, $"/account/external-login/{provider.Provider.ToLowerInvariant()}")))
            .ToArray();
    }

    public bool SignupEnabled { get; }

    public string ReturnUrl { get; }

    public string EscapedReturnUrl { get; }

    public string? Error { get; }

    public string RegisterPath { get; }

    public IReadOnlyList<AuthExternalLoginButtonViewModel> ExternalProviders { get; }
}

internal sealed class AuthRegisterPageViewModel
{
    public AuthRegisterPageViewModel(AuthOptions options, string? returnUrl, string? error = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        SignupEnabled = options.SignupEnabled;
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        Error = error;
        LoginPath = AuthPagePaths.AccountPath(options, "/account/login");
        EscapedReturnUrl = AuthPagePaths.EscapedReturnUrl(ReturnUrl);
        ExternalProviders = options.ExternalProviders.ConfiguredProviders
            .Select(provider => new AuthExternalLoginButtonViewModel(
                provider.DisplayName,
                AuthPagePaths.AccountPath(options, $"/account/external-login/{provider.Provider.ToLowerInvariant()}")))
            .ToArray();
    }

    public bool SignupEnabled { get; }

    public string ReturnUrl { get; }

    public string EscapedReturnUrl { get; }

    public string? Error { get; }

    public string LoginPath { get; }

    public IReadOnlyList<AuthExternalLoginButtonViewModel> ExternalProviders { get; }
}

internal sealed class AuthLoginTwoFactorPageViewModel
{
    public AuthLoginTwoFactorPageViewModel(string? returnUrl, string? error = null)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        Error = error;
    }

    public string ReturnUrl { get; }

    public string? Error { get; }
}

internal sealed class AuthTotpSetupPageViewModel
{
    public AuthTotpSetupPageViewModel(string? key, string? returnUrl, string? error = null)
    {
        Key = key ?? "";
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        Error = error;
    }

    public string Key { get; }

    public string ReturnUrl { get; }

    public string? Error { get; }
}
