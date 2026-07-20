using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthRealmConfiguration : IEntityTypeConfiguration<AuthRealm>
{
    public void Configure(EntityTypeBuilder<AuthRealm> builder)
    {
        builder.ToTable("AuthRealms");

        builder.HasKey(realm => realm.Id);

        builder.Property(realm => realm.Id)
            .ValueGeneratedNever();

        builder.Property(realm => realm.Name)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(realm => realm.Issuer)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(realm => realm.PathBase)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(realm => realm.MfaPolicy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(realm => realm.SignupEnabled)
            .IsRequired();

        builder.Property(realm => realm.CreatedAt)
            .IsRequired();

        builder.Property(realm => realm.UpdatedAt);

        builder.HasIndex(realm => realm.Name)
            .IsUnique();

        builder.HasIndex(realm => realm.PathBase)
            .IsUnique();

        builder.HasMany(realm => realm.Clients)
            .WithOne(client => client.Realm)
            .HasForeignKey(client => client.RealmId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
