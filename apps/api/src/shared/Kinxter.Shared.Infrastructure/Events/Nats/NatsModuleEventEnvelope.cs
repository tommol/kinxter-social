namespace Kinxter.Shared.Infrastructure.Events.Nats;

internal sealed record NatsModuleEventEnvelope(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string EventType,
    string Payload);
