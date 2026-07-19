using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Accounts.Infrastructure.Outbox;

internal sealed class AccountsOutboxWriter : IOutboxWriter<AccountsOutbox>
{
    private const string ModuleName = "accounts";

    private readonly AccountsDbContext dbContext;
    private readonly IOutboxEventSerializer serializer;
    private readonly IClock clock;

    public AccountsOutboxWriter(
        AccountsDbContext dbContext,
        IOutboxEventSerializer serializer,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(clock);

        this.dbContext = dbContext;
        this.serializer = serializer;
        this.clock = clock;
    }

    public async Task AddAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
        where TEvent : IModuleEvent
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        var serializedEvent = this.serializer.Serialize(moduleEvent);
        var now = this.clock.UtcNow;

        var message = new OutboxMessage(
            Guid.CreateVersion7(now),
            moduleEvent.EventId,
            ModuleName,
            serializedEvent.EventType,
            serializedEvent.Payload,
            moduleEvent.OccurredAt,
            now);

        await this.dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
