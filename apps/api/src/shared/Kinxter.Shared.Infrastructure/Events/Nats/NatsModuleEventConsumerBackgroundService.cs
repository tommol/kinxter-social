using System.Text.Json;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;

namespace Kinxter.Shared.Infrastructure.Events.Nats;

internal sealed class NatsModuleEventConsumerBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly NatsJetStreamManager jetStreamManager;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly NatsModuleEventOptions options;
    private readonly ILogger<NatsModuleEventConsumerBackgroundService> logger;

    public NatsModuleEventConsumerBackgroundService(
        NatsJetStreamManager jetStreamManager,
        IServiceScopeFactory scopeFactory,
        IOptions<NatsModuleEventOptions> options,
        ILogger<NatsModuleEventConsumerBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(jetStreamManager);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        this.jetStreamManager = jetStreamManager;
        this.scopeFactory = scopeFactory;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumer = await this.jetStreamManager.CreateOrUpdateConsumerAsync(stoppingToken);

                await foreach (var message in consumer.ConsumeAsync<string>(cancellationToken: stoppingToken))
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "NATS JetStream module event consumer stopped unexpectedly. Retrying in {Delay}.",
                    this.options.ReconnectDelay);

                await Task.Delay(this.options.ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(INatsJSMsg<string> message, CancellationToken cancellationToken)
    {
        try
        {
            message.EnsureSuccess();

            var envelope = DeserializeEnvelope(message.Data);

            using var scope = this.scopeFactory.CreateScope();

            var serializer = scope.ServiceProvider.GetRequiredService<IOutboxEventSerializer>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();
            var moduleEvent = serializer.Deserialize(envelope.EventType, envelope.Payload);

            await dispatcher.DispatchAsync(moduleEvent, cancellationToken);
            await message.AckAsync(cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            this.logger.LogError(
                exception,
                "Discarding malformed NATS module event message on subject {Subject}.",
                message.Subject);

            await message.AckTerminateAsync(
                new AckOpts
                {
                    TerminateReason = "Malformed module event envelope."
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Failed to process NATS module event message on subject {Subject}.",
                message.Subject);

            await message.NakAsync(
                new AckOpts
                {
                    NakDelay = this.options.ReconnectDelay
                },
                cancellationToken);
        }
    }

    private static NatsModuleEventEnvelope DeserializeEnvelope(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new JsonException("NATS module event message payload is empty.");
        }

        return JsonSerializer.Deserialize<NatsModuleEventEnvelope>(payload, JsonSerializerOptions)
            ?? throw new JsonException("NATS module event message envelope cannot be deserialized.");
    }
}
