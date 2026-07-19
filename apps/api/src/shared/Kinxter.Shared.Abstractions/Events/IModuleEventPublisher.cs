namespace Kinxter.Shared.Abstractions.Events;

public interface IModuleEventPublisher
{
    Task PublishAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken = default);

    Task PublishAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
        where TEvent : IModuleEvent;
}
