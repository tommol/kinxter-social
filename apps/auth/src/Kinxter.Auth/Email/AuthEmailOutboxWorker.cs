using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Auth.Email;

internal sealed class AuthEmailOutboxWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AuthEmailOutboxWorker> logger;

    public AuthEmailOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<AuthEmailOutboxWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessAsync(stoppingToken);
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessAsync(stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = this.scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IAuthEmailSender>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var messages = await dbContext.EmailOutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(20)
            .ToArrayAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await sender.SendAsync(message, cancellationToken);
                message.MarkProcessed(clock.UtcNow);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Failed to deliver auth email message {MessageId}.", message.Id);
                message.MarkFailed(clock.UtcNow, exception.Message);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
