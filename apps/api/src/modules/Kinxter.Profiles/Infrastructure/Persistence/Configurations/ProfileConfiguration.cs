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
            .HasMaxLength(Profile.HandleMaxLength);

        builder.Property(profile => profile.NormalizedHandle)
            .IsRequired()
            .HasMaxLength(Profile.HandleMaxLength);

        builder.Property(profile => profile.DisplayName)
            .IsRequired()
            .HasMaxLength(Profile.DisplayNameMaxLength);

        builder.Property(profile => profile.Bio)
            .HasMaxLength(Profile.BioMaxLength);

        builder.Property(profile => profile.ProfilePictureUrl)
            .HasMaxLength(Profile.ProfilePictureUrlMaxLength);

        builder.Property(profile => profile.CreatedAt)
            .IsRequired();

        builder.HasIndex(profile => profile.AccountId)
            .IsUnique();

        builder.HasIndex(profile => profile.NormalizedHandle)
            .IsUnique();
    }
}
