namespace Kinxter.Shared.Abstractions.Events;

public abstract record ModuleEvent(Guid EventId, DateTimeOffset OccurredAt) : IModuleEvent;
