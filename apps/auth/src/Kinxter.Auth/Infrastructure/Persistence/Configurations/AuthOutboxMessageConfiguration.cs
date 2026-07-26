using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthOutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("AuthOutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.EventId).IsRequired();
        builder.Property(message => message.ModuleName).IsRequired().HasMaxLength(128);
        builder.Property(message => message.EventType).IsRequired().HasMaxLength(512);
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.OccurredAt).IsRequired();
        builder.Property(message => message.CreatedAt).IsRequired();
        builder.Property(message => message.Error).HasMaxLength(2000);
        builder.HasIndex(message => message.EventId).IsUnique();
        builder.HasIndex(message => new { message.ProcessedAt, message.CreatedAt });
    }
}
