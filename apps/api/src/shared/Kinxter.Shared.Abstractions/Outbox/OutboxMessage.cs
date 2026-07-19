namespace Kinxter.Shared.Abstractions.Outbox;

public sealed record OutboxMessage(
    Guid Id,
    Guid EventId,
    string ModuleName,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt = null,
    DateTimeOffset? LastAttemptedAt = null,
    int RetryCount = 0,
    string? Error = null);
