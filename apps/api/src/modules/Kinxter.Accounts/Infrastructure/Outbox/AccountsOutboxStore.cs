using Kinxter.Accounts.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Accounts.Infrastructure.Outbox;

internal sealed class AccountsOutboxStore : IOutboxStore
{
    private readonly AccountsDbContext dbContext;

    public AccountsOutboxStore(AccountsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    public OutboxModuleDescriptor Module { get; } = new("accounts", "accounts");

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await this.dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await this.dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(
        Guid messageId,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken = default)
    {
        await this.dbContext.OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.ProcessedAt, processedAt)
                    .SetProperty(message => message.LastAttemptedAt, processedAt)
                    .SetProperty(message => message.Error, (string?)null),
                cancellationToken);
    }

    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken = default)
    {
        await this.dbContext.OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.LastAttemptedAt, attemptedAt)
                    .SetProperty(message => message.RetryCount, message => message.RetryCount + 1)
                    .SetProperty(message => message.Error, error),
                cancellationToken);
    }
}
