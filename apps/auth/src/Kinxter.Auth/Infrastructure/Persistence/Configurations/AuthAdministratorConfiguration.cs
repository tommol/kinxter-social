using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthAdministratorConfiguration : IEntityTypeConfiguration<AuthAdministrator>
{
    public void Configure(EntityTypeBuilder<AuthAdministrator> builder)
    {
        builder.ToTable("AuthAdministrators");

        builder.HasKey(administrator => administrator.Id);

        builder.Property(administrator => administrator.Id)
            .ValueGeneratedNever();

        builder.Property(administrator => administrator.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(administrator => administrator.NormalizedUsername)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(administrator => administrator.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(administrator => administrator.Enabled)
            .IsRequired();

        builder.Property(administrator => administrator.CreatedAt)
            .IsRequired();

        builder.Property(administrator => administrator.UpdatedAt);
        builder.Property(administrator => administrator.LastSignedInAt);

        builder.HasIndex(administrator => administrator.NormalizedUsername)
            .IsUnique();
    }
}
