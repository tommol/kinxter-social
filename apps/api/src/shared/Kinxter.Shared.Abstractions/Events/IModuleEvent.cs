namespace Kinxter.Shared.Abstractions.Events;

public interface IModuleEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
