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
        Clients = realm.Clients;
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

    public IReadOnlyList<AuthAdminClientSummary> Clients { get; }

    public string? Error { get; }

    public bool Saved { get; }
}

internal sealed class AuthAdminClientPageViewModel
{
    public AuthAdminClientPageViewModel(
        string username,
        string controlPath,
        string logoutPath,
        string antiforgeryToken,
        AuthAdminRealmDetails realm,
        AuthAdminClientDetails? client = null,
        AuthAdminCreateClientCommand? attemptedCreate = null,
        AuthAdminUpdateClientCommand? attemptedUpdate = null,
        string? error = null,
        string? clientSecret = null,
        bool saved = false)
    {
        Username = username;
        ControlPath = controlPath;
        LogoutPath = logoutPath;
        AntiforgeryToken = antiforgeryToken;
        RealmId = realm.Id;
        RealmName = realm.Name;
        ClientDatabaseId = client?.Id;
        ClientId = attemptedCreate?.ClientId ?? client?.ClientId ?? "";
        DisplayName = attemptedCreate?.DisplayName ?? attemptedUpdate?.DisplayName ?? client?.DisplayName ?? "";
        Enabled = attemptedUpdate?.Enabled ?? client?.Enabled ?? true;
        ClientSecretConfigured = client?.ClientSecretConfigured ?? false;
        RedirectUris = attemptedCreate?.RedirectUris ?? attemptedUpdate?.RedirectUris ?? client?.RedirectUris ?? [];
        PostLogoutRedirectUris = attemptedCreate?.PostLogoutRedirectUris ?? attemptedUpdate?.PostLogoutRedirectUris ?? client?.PostLogoutRedirectUris ?? [];
        Scopes = attemptedCreate?.Scopes ?? attemptedUpdate?.Scopes ?? client?.Scopes ?? ["openid", "profile", "email"];
        CreatedAt = client?.CreatedAt;
        UpdatedAt = client?.UpdatedAt;
        Error = error;
        ClientSecret = clientSecret;
        Saved = saved;
    }

    public string Username { get; }

    public string ControlPath { get; }

    public string LogoutPath { get; }

    public string AntiforgeryToken { get; }

    public Guid RealmId { get; }

    public string RealmName { get; }

    public Guid? ClientDatabaseId { get; }

    public bool IsNew => ClientDatabaseId is null;

    public string ClientId { get; }

    public string DisplayName { get; }

    public bool Enabled { get; }

    public bool ClientSecretConfigured { get; }

    public string[] RedirectUris { get; }

    public string[] PostLogoutRedirectUris { get; }

    public string[] Scopes { get; }

    public DateTimeOffset? CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; }

    public string? Error { get; }

    public string? ClientSecret { get; }

    public bool Saved { get; }
}
