using System.Text.Json;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;

namespace Kinxter.Shared.Infrastructure.Events.Nats;

internal sealed class NatsModuleEventPublisher : IModuleEventPublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly NatsJetStreamContextProvider contextProvider;
    private readonly NatsJetStreamManager jetStreamManager;
    private readonly IOutboxEventSerializer serializer;
    private readonly NatsModuleEventOptions options;

    public NatsModuleEventPublisher(
        NatsJetStreamContextProvider contextProvider,
        NatsJetStreamManager jetStreamManager,
        IOutboxEventSerializer serializer,
        IOptions<NatsModuleEventOptions> options)
    {
        ArgumentNullException.ThrowIfNull(contextProvider);
        ArgumentNullException.ThrowIfNull(jetStreamManager);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(options);

        this.contextProvider = contextProvider;
        this.jetStreamManager = jetStreamManager;
        this.serializer = serializer;
        this.options = options.Value;
    }

    public Task PublishAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return PublishInternalAsync(moduleEvent, cancellationToken);
    }

    public Task PublishAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
        where TEvent : IModuleEvent
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return PublishInternalAsync(moduleEvent, cancellationToken);
    }

    private async Task PublishInternalAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken)
    {
        var serializedEvent = this.serializer.Serialize(moduleEvent);
        var envelope = new NatsModuleEventEnvelope(
            moduleEvent.EventId,
            moduleEvent.OccurredAt,
            serializedEvent.EventType,
            serializedEvent.Payload);

        var subject = NatsModuleEventSubject.FromEventType(
            this.options.SubjectPrefix,
            moduleEvent.GetType());

        await this.jetStreamManager.EnsureStreamAsync(cancellationToken);

        var ack = await this.contextProvider.Context.PublishAsync(
            subject,
            JsonSerializer.Serialize(envelope, JsonSerializerOptions),
            opts: new NatsJSPubOpts
            {
                ExpectedStream = this.options.StreamName,
                MsgId = moduleEvent.EventId.ToString("N")
            },
            cancellationToken: cancellationToken);

        ack.EnsureSuccess();
    }
}
