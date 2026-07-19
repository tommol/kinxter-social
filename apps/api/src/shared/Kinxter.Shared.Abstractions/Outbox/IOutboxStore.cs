namespace Kinxter.Shared.Abstractions.Outbox;

public interface IOutboxStore
{
    OutboxModuleDescriptor Module { get; }

    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(
        Guid messageId,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken = default);
}
