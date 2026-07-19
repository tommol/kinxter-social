using Kinxter.Shared.Abstractions.Events;

namespace Kinxter.Shared.Abstractions.Outbox;

public interface IOutboxWriter
{
    Task AddAsync<TEvent>(TEvent moduleEvent, CancellationToken cancellationToken = default)
        where TEvent : IModuleEvent;
}

public interface IOutboxWriter<TModule> : IOutboxWriter;
