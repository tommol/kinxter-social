using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Kinxter.Shared.Infrastructure.Events.Nats;

internal sealed class NatsJetStreamManager
{
    private readonly NatsJetStreamContextProvider contextProvider;
    private readonly NatsModuleEventOptions options;
    private readonly SemaphoreSlim streamSemaphore = new(1, 1);
    private volatile bool streamInitialized;

    public NatsJetStreamManager(
        NatsJetStreamContextProvider contextProvider,
        IOptions<NatsModuleEventOptions> options)
    {
        ArgumentNullException.ThrowIfNull(contextProvider);
        ArgumentNullException.ThrowIfNull(options);

        this.contextProvider = contextProvider;
        this.options = options.Value;
    }

    public async Task EnsureStreamAsync(CancellationToken cancellationToken = default)
    {
        if (this.streamInitialized)
        {
            return;
        }

        await this.streamSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (this.streamInitialized)
            {
                return;
            }

            await this.contextProvider.Context.CreateOrUpdateStreamAsync(
                new StreamConfig(
                    this.options.StreamName,
                    [NatsModuleEventSubject.All(this.options.SubjectPrefix)])
                {
                    Description = "Kinxter module integration events",
                    DuplicateWindow = this.options.DuplicateWindow
                },
                cancellationToken);

            this.streamInitialized = true;
        }
        finally
        {
            this.streamSemaphore.Release();
        }
    }

    public async Task<INatsJSConsumer> CreateOrUpdateConsumerAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStreamAsync(cancellationToken);

        return await this.contextProvider.Context.CreateOrUpdateConsumerAsync(
            this.options.StreamName,
            new ConsumerConfig(this.options.ConsumerName)
            {
                FilterSubject = NatsModuleEventSubject.All(this.options.SubjectPrefix),
                AckWait = this.options.AckWait,
                MaxAckPending = this.options.MaxAckPending,
                MaxDeliver = this.options.MaxDeliver
            },
            cancellationToken);
    }
}
