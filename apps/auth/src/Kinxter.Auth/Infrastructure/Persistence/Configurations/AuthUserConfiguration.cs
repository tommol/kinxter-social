using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthUserConfiguration : IEntityTypeConfiguration<AuthUser>
{
    public void Configure(EntityTypeBuilder<AuthUser> builder)
    {
        builder.Property(user => user.Realm)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.Property(user => user.DisabledAt);
        builder.Property(user => user.DeletedAt);

        builder.HasIndex(user => user.NormalizedUserName)
            .IsUnique(false)
            .HasDatabaseName("IX_AspNetUsers_NormalizedUserName");

        builder.HasIndex(user => new
            {
                user.Realm,
                user.NormalizedUserName
            })
            .IsUnique()
            .HasDatabaseName("UserNameIndex");

        builder.HasIndex(user => new
            {
                user.Realm,
                user.NormalizedEmail
            })
            .IsUnique();
    }
}
