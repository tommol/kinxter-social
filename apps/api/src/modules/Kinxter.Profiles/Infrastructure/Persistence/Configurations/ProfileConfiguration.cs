using Kinxter.Profiles.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Profiles.Infrastructure.Persistence.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .ValueGeneratedNever();

        builder.Property(profile => profile.AccountId)
            .IsRequired();

        builder.Property(profile => profile.Handle)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(profile => profile.NormalizedHandle)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(profile => profile.DisplayName)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(profile => profile.CreatedAt)
            .IsRequired();

        builder.HasIndex(profile => profile.AccountId)
            .IsUnique();

        builder.HasIndex(profile => profile.NormalizedHandle)
            .IsUnique();
    }
}
