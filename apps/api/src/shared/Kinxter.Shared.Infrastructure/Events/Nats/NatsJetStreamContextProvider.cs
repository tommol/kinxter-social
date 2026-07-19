using NATS.Client.JetStream;
using NATS.Net;

namespace Kinxter.Shared.Infrastructure.Events.Nats;

internal sealed class NatsJetStreamContextProvider
{
    public NatsJetStreamContextProvider(NatsClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        Context = client.CreateJetStreamContext();
    }

    public INatsJSContext Context { get; }
}
