using Kinxter.Shared.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Shared.Infrastructure.Events;

internal sealed class ModuleEventDispatcher : IModuleEventDispatcher
{
    private readonly IServiceProvider serviceProvider;

    public ModuleEventDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.serviceProvider = serviceProvider;
    }

    public Task DispatchAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleEvent);

        return DispatchAsync(moduleEvent.GetType(), moduleEvent, cancellationToken);
    }

    private async Task DispatchAsync(Type eventType, IModuleEvent moduleEvent, CancellationToken cancellationToken)
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
