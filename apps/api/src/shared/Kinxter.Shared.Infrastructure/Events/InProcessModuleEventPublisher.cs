using Kinxter.Shared.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Shared.Infrastructure.Events;

internal sealed class InProcessModuleEventPublisher : IModuleEventPublisher
{
    private readonly IServiceProvider serviceProvider;

    public InProcessModuleEventPublisher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.serviceProvider = serviceProvider;
    }

    public Task PublishAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return PublishAsync(moduleEvent.GetType(), moduleEvent, cancellationToken);
    }

    public Task PublishAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
        where TEvent : IModuleEvent
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return PublishAsync(typeof(TEvent), moduleEvent, cancellationToken);
    }

    private async Task PublishAsync(Type eventType, IModuleEvent moduleEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IModuleEventHandler<>).MakeGenericType(eventType);
        var handlers = this.serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod(nameof(IModuleEventHandler<IModuleEvent>.HandleAsync));

            if (handleMethod is null)
            {
                continue;
            }

            var task = (Task?)handleMethod.Invoke(handler, [moduleEvent, cancellationToken]);

            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
