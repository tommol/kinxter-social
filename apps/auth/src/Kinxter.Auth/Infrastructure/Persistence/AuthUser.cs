using Microsoft.AspNetCore.Identity;

namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthUser : IdentityUser<Guid>
{
    public string Realm { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DisabledAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
