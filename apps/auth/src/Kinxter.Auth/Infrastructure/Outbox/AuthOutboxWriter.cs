using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Auth.Infrastructure.Outbox;

internal sealed class AuthOutboxWriter : IOutboxWriter<AuthOutbox>
{
    private readonly AuthDbContext dbContext;
    private readonly IOutboxEventSerializer serializer;
    private readonly IClock clock;

    public AuthOutboxWriter(AuthDbContext dbContext, IOutboxEventSerializer serializer, IClock clock)
    {
        this.dbContext = dbContext;
        this.serializer = serializer;
        this.clock = clock;
    }

    public Task AddAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
        where TEvent : IModuleEvent
    {
        var serialized = this.serializer.Serialize(moduleEvent);
        var now = this.clock.UtcNow;
        var message = new OutboxMessage(
            Guid.CreateVersion7(now),
            moduleEvent.EventId,
            "auth",
            serialized.EventType,
            serialized.Payload,
            moduleEvent.OccurredAt,
            now);

        return this.dbContext.OutboxMessages.AddAsync(message, cancellationToken).AsTask();
    }
}
