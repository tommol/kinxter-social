namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthRealm
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Issuer { get; set; } = "";

    public string PathBase { get; set; } = "";

    public AuthMfaPolicy MfaPolicy { get; set; } = AuthMfaPolicy.OptionalStepUp;

    public bool SignupEnabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public List<AuthClient> Clients { get; set; } = [];
}
