namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthAdministrator
{
    public Guid Id { get; set; }

    public string Username { get; set; } = "";

    public string NormalizedUsername { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? LastSignedInAt { get; set; }
}
