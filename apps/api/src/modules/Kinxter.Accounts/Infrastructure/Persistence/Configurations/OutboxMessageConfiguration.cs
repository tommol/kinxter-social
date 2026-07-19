using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Accounts.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(OutboxDefaults.TableName);

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedNever();

        builder.Property(message => message.EventId)
            .IsRequired();

        builder.Property(message => message.ModuleName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(message => message.EventType)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(message => message.Payload)
            .IsRequired();

        builder.Property(message => message.OccurredAt)
            .IsRequired();

        builder.Property(message => message.CreatedAt)
            .IsRequired();

        builder.Property(message => message.RetryCount)
            .IsRequired();

        builder.Property(message => message.Error)
            .HasMaxLength(4096);

        builder.HasIndex(message => message.EventId)
            .IsUnique();

        builder.HasIndex(message => new
        {
            message.ProcessedAt,
            message.CreatedAt
        });
    }
}
