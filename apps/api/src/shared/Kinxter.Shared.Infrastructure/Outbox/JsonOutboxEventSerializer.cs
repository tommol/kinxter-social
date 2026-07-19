using System.Text.Json;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;

namespace Kinxter.Shared.Infrastructure.Outbox;

internal sealed class JsonOutboxEventSerializer : IOutboxEventSerializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public SerializedOutboxEvent Serialize<TEvent>(TEvent moduleEvent)
        where TEvent : IModuleEvent
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        var eventType = typeof(TEvent).AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Event type '{typeof(TEvent).FullName}' cannot be serialized.");

        var payload = JsonSerializer.Serialize(moduleEvent, typeof(TEvent), JsonSerializerOptions);

        return new SerializedOutboxEvent(eventType, payload);
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
