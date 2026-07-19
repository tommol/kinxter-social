using Kinxter.Accounts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Accounts.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .ValueGeneratedNever();

        builder.Property(account => account.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(account => account.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(account => account.IdentityProvider)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(account => account.IdentitySubject)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(account => account.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(account => account.CreatedAt)
            .IsRequired();

        builder.HasIndex(account => account.NormalizedEmail)
            .IsUnique();

        builder.HasIndex(account => new
            {
                account.IdentityProvider,
                account.IdentitySubject
            })
            .IsUnique();
    }
}
