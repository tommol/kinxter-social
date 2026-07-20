using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthClientConfiguration : IEntityTypeConfiguration<AuthClient>
{
    public void Configure(EntityTypeBuilder<AuthClient> builder)
    {
        builder.ToTable("AuthClients");

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .ValueGeneratedNever();

        builder.Property(client => client.ClientId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(client => client.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(client => client.Enabled)
            .IsRequired();

        builder.Property(client => client.ClientSecretConfigured)
            .IsRequired();

        builder.Property(client => client.RedirectUris)
            .IsRequired();

        builder.Property(client => client.PostLogoutRedirectUris)
            .IsRequired();

        builder.Property(client => client.Scopes)
            .IsRequired();

        builder.Property(client => client.CreatedAt)
            .IsRequired();

        builder.Property(client => client.UpdatedAt);

        builder.HasIndex(client => client.ClientId)
            .IsUnique();

        builder.HasIndex(client => new
            {
                client.RealmId,
                client.ClientId
            })
            .IsUnique();
    }
}
