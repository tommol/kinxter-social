using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext : IdentityDbContext<AuthUser, IdentityRole<Guid>, Guid>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuthUser>(user =>
        {
            user.Property(current => current.Realm)
                .IsRequired()
                .HasMaxLength(64);

            user.Property(current => current.CreatedAt)
                .IsRequired();

            user.Property(current => current.DisabledAt);
            user.Property(current => current.DeletedAt);

            user.HasIndex(current => new
                {
                    current.Realm,
                    current.NormalizedEmail
                })
                .IsUnique();
        });
    }
}
