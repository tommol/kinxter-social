namespace Kinxter.Shared.Abstractions.Events;

public interface IModuleEventHandler<in TEvent>
    where TEvent : IModuleEvent
{
    Task HandleAsync(TEvent moduleEvent, CancellationToken cancellationToken = default);
}
