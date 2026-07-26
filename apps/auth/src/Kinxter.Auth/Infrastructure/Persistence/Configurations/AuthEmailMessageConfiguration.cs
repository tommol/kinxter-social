using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthEmailMessageConfiguration : IEntityTypeConfiguration<AuthEmailMessage>
{
    public void Configure(EntityTypeBuilder<AuthEmailMessage> builder)
    {
        builder.ToTable("AuthEmailOutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Recipient).IsRequired().HasMaxLength(320);
        builder.Property(message => message.Subject).IsRequired().HasMaxLength(300);
        builder.Property(message => message.HtmlBody).IsRequired();
        builder.Property(message => message.TextBody).IsRequired();
        builder.Property(message => message.CreatedAt).IsRequired();
        builder.Property(message => message.Error).HasMaxLength(2000);
        builder.HasIndex(message => new { message.ProcessedAt, message.CreatedAt });
    }
}
