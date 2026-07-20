using System.Text.Json;
using System.Reflection;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;

namespace Kinxter.Shared.Infrastructure.Outbox;

internal sealed class JsonOutboxEventSerializer : IOutboxEventSerializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly object RegistryLock = new();
    private static IReadOnlyDictionary<string, Type>? eventTypeRegistry;

    public SerializedOutboxEvent Serialize(IModuleEvent moduleEvent)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        var eventType = moduleEvent.GetType();
        var serializedEventType = GetEventName(eventType);

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

        var type = ResolveEventType(eventType);

        if (!typeof(IModuleEvent).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Event type '{eventType}' does not implement {nameof(IModuleEvent)}.");
        }

        return (IModuleEvent?)JsonSerializer.Deserialize(payload, type, JsonSerializerOptions)
            ?? throw new InvalidOperationException($"Event payload for '{eventType}' cannot be deserialized.");
    }

    private static string GetEventName(Type eventType)
    {
        return eventType.GetCustomAttributes(typeof(ModuleEventNameAttribute), inherit: false)
            .OfType<ModuleEventNameAttribute>()
            .SingleOrDefault()
            ?.Name
            ?? eventType.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Event type '{eventType.FullName}' cannot be serialized.");
    }

    private static Type ResolveEventType(string eventType)
    {
        var resolvedType = Type.GetType(eventType, throwOnError: false);

        if (resolvedType is not null)
        {
            return resolvedType;
        }

        var registry = GetEventTypeRegistry();

        return registry.TryGetValue(eventType, out var registeredType)
            ? registeredType
            : throw new InvalidOperationException($"Event type '{eventType}' cannot be resolved.");
    }

    private static IReadOnlyDictionary<string, Type> GetEventTypeRegistry()
    {
        if (eventTypeRegistry is not null)
        {
            return eventTypeRegistry;
        }

        lock (RegistryLock)
        {
            if (eventTypeRegistry is not null)
            {
                return eventTypeRegistry;
            }

            eventTypeRegistry = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        return exception.Types.OfType<Type>();
                    }
                })
                .Where(type => !type.IsAbstract && typeof(IModuleEvent).IsAssignableFrom(type))
                .Select(type => new
                {
                    Type = type,
                    Name = type.GetCustomAttributes(typeof(ModuleEventNameAttribute), inherit: false)
                        .OfType<ModuleEventNameAttribute>()
                        .SingleOrDefault()
                        ?.Name
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                .ToDictionary(entry => entry.Name!, entry => entry.Type, StringComparer.Ordinal);

            return eventTypeRegistry;
        }
    }
}
