using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kinxter.Shared.Infrastructure.Outbox;

internal sealed class OutboxProcessorBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IClock clock;
    private readonly ILogger<OutboxProcessorBackgroundService> logger;

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        this.scopeFactory = scopeFactory;
        this.clock = clock;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessOutboxAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOutboxAsync(stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = this.scopeFactory.CreateScope();

        IReadOnlyCollection<IOutboxStore> stores;

        try
        {
            stores = scope.ServiceProvider.GetServices<IOutboxStore>().ToList();
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Failed to resolve outbox stores.");

            return;
        }

        var serializer = scope.ServiceProvider.GetRequiredService<IOutboxEventSerializer>();
        var publisher = scope.ServiceProvider.GetRequiredService<IModuleEventPublisher>();

        foreach (var store in stores)
        {
            await ProcessStoreAsync(store, serializer, publisher, cancellationToken);
        }
    }

    private async Task ProcessStoreAsync(
        IOutboxStore store,
        IOutboxEventSerializer serializer,
        IModuleEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<OutboxMessage> messages;

        try
        {
            messages = await store.GetPendingAsync(BatchSize, cancellationToken);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to read outbox messages for module {ModuleName}.",
                store.Module.ModuleName);

            return;
        }

        foreach (var message in messages)
        {
            await ProcessMessageAsync(store, serializer, publisher, message, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        IOutboxStore store,
        IOutboxEventSerializer serializer,
        IModuleEventPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var moduleEvent = serializer.Deserialize(message.EventType, message.Payload);

            await publisher.PublishAsync(moduleEvent, cancellationToken);
            await store.MarkAsProcessedAsync(message.Id, this.clock.UtcNow, cancellationToken);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to process outbox message {MessageId} for module {ModuleName}.",
                message.Id,
                store.Module.ModuleName);

            await store.MarkAsFailedAsync(
                message.Id,
                exception.Message,
                this.clock.UtcNow,
                cancellationToken);
        }
    }
}
