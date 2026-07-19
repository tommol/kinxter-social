using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.Shared.Infrastructure.Events;

internal interface IModuleEventDispatcher
{
    Task DispatchAsync(IModuleEvent moduleEvent, CancellationToken cancellationToken = default);
}
