namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthClient
{
    public Guid Id { get; set; }

    public Guid RealmId { get; set; }

    public AuthRealm Realm { get; set; } = null!;

    public string ClientId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public bool ClientSecretConfigured { get; set; }

    public string[] RedirectUris { get; set; } = [];

    public string[] PostLogoutRedirectUris { get; set; } = [];

    public string[] Scopes { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
