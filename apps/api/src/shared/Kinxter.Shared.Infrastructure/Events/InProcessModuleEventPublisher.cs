using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.Shared.Infrastructure.Events;

internal sealed class InProcessModuleEventPublisher : IModuleEventPublisher
{
    private readonly IModuleEventDispatcher dispatcher;

    public InProcessModuleEventPublisher(IModuleEventDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        this.dispatcher = dispatcher;
    }

    public Task PublishAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return this.dispatcher.DispatchAsync(moduleEvent, cancellationToken);
    }

    public Task PublishAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
        where TEvent : IModuleEvent
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return this.dispatcher.DispatchAsync(moduleEvent, cancellationToken);
    }
}
