using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth.Infrastructure.Outbox;

internal sealed class AuthOutboxStore : IOutboxStore
{
    private readonly AuthDbContext dbContext;

    public AuthOutboxStore(AuthDbContext dbContext) => this.dbContext = dbContext;

    public OutboxModuleDescriptor Module { get; } = new("auth", "auth");

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        this.dbContext.OutboxMessages.AddAsync(message, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await this.dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

    public Task MarkAsProcessedAsync(Guid messageId, DateTimeOffset processedAt, CancellationToken cancellationToken = default) =>
        this.dbContext.OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.ProcessedAt, processedAt)
                .SetProperty(message => message.LastAttemptedAt, processedAt)
                .SetProperty(message => message.Error, (string?)null), cancellationToken);

    public Task MarkAsFailedAsync(Guid messageId, string error, DateTimeOffset attemptedAt, CancellationToken cancellationToken = default) =>
        this.dbContext.OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.LastAttemptedAt, attemptedAt)
                .SetProperty(message => message.RetryCount, message => message.RetryCount + 1)
                .SetProperty(message => message.Error, error.Length <= 2000 ? error : error[..2000]), cancellationToken);
}
