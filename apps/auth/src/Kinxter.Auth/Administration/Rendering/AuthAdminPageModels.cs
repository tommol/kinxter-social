namespace Kinxter.Auth.Administration;

internal sealed record AuthAdminLoginPageViewModel(
    string LoginPath,
    string ReturnUrl,
    string AntiforgeryToken,
    string? Error);

internal sealed record AuthAdminDashboardPageViewModel(
    string Username,
    string ControlPath,
    string LogoutPath,
    string AntiforgeryToken,
    IReadOnlyList<AuthAdminRealmSummary> Realms);

internal sealed class AuthAdminRealmPageViewModel
{
    public AuthAdminRealmPageViewModel(
        string username,
        string controlPath,
        string logoutPath,
        string antiforgeryToken,
        AuthAdminRealmDetails realm,
        AuthAdminUpdateRealmCommand? attemptedUpdate = null,
        string? error = null,
        bool saved = false)
    {
        Username = username;
        ControlPath = controlPath;
        LogoutPath = logoutPath;
        AntiforgeryToken = antiforgeryToken;
        RealmId = realm.Id;
        Name = realm.Name;
        Issuer = attemptedUpdate?.Issuer ?? realm.Issuer;
        PathBase = attemptedUpdate?.PathBase ?? realm.PathBase;
        MfaPolicy = attemptedUpdate?.MfaPolicy ?? realm.MfaPolicy;
        SignupEnabled = attemptedUpdate?.SignupEnabled ?? realm.SignupEnabled;
        CreatedAt = realm.CreatedAt;
        UpdatedAt = realm.UpdatedAt;
        Error = error;
        Saved = saved;
    }

    public string Username { get; }

    public string ControlPath { get; }

    public string LogoutPath { get; }

    public string AntiforgeryToken { get; }

    public Guid RealmId { get; }

    public string Name { get; }

    public string Issuer { get; }

    public string PathBase { get; }

    public AuthMfaPolicy MfaPolicy { get; }

    public bool SignupEnabled { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; }

    public string? Error { get; }

    public bool Saved { get; }
}
