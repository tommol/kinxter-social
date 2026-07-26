using Kinxter.Accounts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Accounts.Infrastructure.Persistence.Configurations;

internal sealed class AccountConsentConfiguration : IEntityTypeConfiguration<AccountConsent>
{
    public void Configure(EntityTypeBuilder<AccountConsent> builder)
    {
        builder.ToTable("account_consents");
        builder.HasKey(consent => consent.Id);
        builder.Property(consent => consent.Id).ValueGeneratedNever();
        builder.Property(consent => consent.AccountId).IsRequired();
        builder.Property(consent => consent.AdultConfirmed).IsRequired();
        builder.Property(consent => consent.TermsVersion).IsRequired().HasMaxLength(64);
        builder.Property(consent => consent.PrivacyVersion).IsRequired().HasMaxLength(64);
        builder.Property(consent => consent.Locale).IsRequired().HasMaxLength(8);
        builder.Property(consent => consent.AcceptedAt).IsRequired();
        builder.HasIndex(consent => new { consent.AccountId, consent.TermsVersion, consent.PrivacyVersion }).IsUnique();
    }
}
