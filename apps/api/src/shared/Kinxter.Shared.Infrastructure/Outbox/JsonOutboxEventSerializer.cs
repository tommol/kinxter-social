using System.Text.Json;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;

namespace Kinxter.Shared.Infrastructure.Outbox;

internal sealed class JsonOutboxEventSerializer : IOutboxEventSerializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public SerializedOutboxEvent Serialize(IModuleEvent moduleEvent)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        var eventType = moduleEvent.GetType();
        var serializedEventType = eventType.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Event type '{eventType.FullName}' cannot be serialized.");

        var payload = JsonSerializer.Serialize(moduleEvent, eventType, JsonSerializerOptions);

        return new SerializedOutboxEvent(serializedEventType, payload);
    }

    public SerializedOutboxEvent Serialize<TEvent>(TEvent moduleEvent)
        where TEvent : IModuleEvent
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return Serialize((IModuleEvent)moduleEvent);
    }

    public IModuleEvent Deserialize(string eventType, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var type = Type.GetType(eventType, throwOnError: true)
            ?? throw new InvalidOperationException($"Event type '{eventType}' cannot be resolved.");

        if (!typeof(IModuleEvent).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Event type '{eventType}' does not implement {nameof(IModuleEvent)}.");
        }

        return (IModuleEvent?)JsonSerializer.Deserialize(payload, type, JsonSerializerOptions)
            ?? throw new InvalidOperationException($"Event payload for '{eventType}' cannot be deserialized.");
    }
}
