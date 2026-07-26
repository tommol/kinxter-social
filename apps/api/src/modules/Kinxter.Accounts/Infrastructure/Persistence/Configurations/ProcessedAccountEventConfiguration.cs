using Kinxter.Accounts.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinxter.Accounts.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedAccountEventConfiguration : IEntityTypeConfiguration<ProcessedAccountEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedAccountEvent> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(message => message.EventId);
        builder.Property(message => message.EventId).ValueGeneratedNever();
        builder.Property(message => message.EventType).IsRequired().HasMaxLength(256);
        builder.Property(message => message.ProcessedAt).IsRequired();
    }
}
