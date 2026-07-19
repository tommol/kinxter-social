namespace Kinxter.Shared.Abstractions.Outbox;

public sealed record SerializedOutboxEvent(string EventType, string Payload);
