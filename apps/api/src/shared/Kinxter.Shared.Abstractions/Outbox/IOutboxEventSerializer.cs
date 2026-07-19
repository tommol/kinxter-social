using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.Shared.Abstractions.Outbox;

public interface IOutboxEventSerializer
{
    SerializedOutboxEvent Serialize<TEvent>(TEvent moduleEvent)
        where TEvent : IModuleEvent;

    IModuleEvent Deserialize(string eventType, string payload);
}
