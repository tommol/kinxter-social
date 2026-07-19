using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Time;
using Kinxter.Shared.Infrastructure.Events;
using Kinxter.Shared.Infrastructure.Outbox;
using Kinxter.Shared.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kinxter.Shared.Infrastructure.DependencyInjection;

public static class SharedInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<IOutboxEventSerializer, JsonOutboxEventSerializer>();
        services.TryAddScoped<IModuleEventPublisher, InProcessModuleEventPublisher>();
        services.AddHostedService<OutboxProcessorBackgroundService>();

        return services;
    }
}
