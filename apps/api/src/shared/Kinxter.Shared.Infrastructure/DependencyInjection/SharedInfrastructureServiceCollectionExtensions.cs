using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Time;
using Kinxter.Shared.Infrastructure.Events;
using Kinxter.Shared.Infrastructure.Events.Nats;
using Kinxter.Shared.Infrastructure.Outbox;
using Kinxter.Shared.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Net;

namespace Kinxter.Shared.Infrastructure.DependencyInjection;

public static class SharedInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        AddSharedDefaults(services);
        services.TryAddScoped<IModuleEventPublisher, InProcessModuleEventPublisher>();
        services.AddHostedService<OutboxProcessorBackgroundService>();

        return services;
    }

    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddSharedDefaults(services);
        AddModuleEventTransport(services, configuration);
        services.AddHostedService<OutboxProcessorBackgroundService>();

        return services;
    }

    private static void AddSharedDefaults(IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<IOutboxEventSerializer, JsonOutboxEventSerializer>();
        services.TryAddScoped<IModuleEventDispatcher, ModuleEventDispatcher>();
    }

    private static void AddModuleEventTransport(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var transport = configuration["ModuleEvents:Transport"];

        if (!string.Equals(transport, ModuleEventTransports.Nats, StringComparison.OrdinalIgnoreCase))
        {
            services.TryAddScoped<IModuleEventPublisher, InProcessModuleEventPublisher>();

            return;
        }

        var options = NatsModuleEventOptions.FromConfiguration(
            configuration.GetSection(NatsModuleEventOptions.SectionName));

        services.AddSingleton(Options.Create(options));
        services.AddSingleton(_ =>
        {
            return new NatsClient(new NatsOpts
            {
                Url = options.Url,
                RetryOnInitialConnect = true,
                DrainSubscriptionsOnDispose = true,
                ConsumerDrainOnDisposeTimeout = TimeSpan.FromSeconds(5)
            });
        });
        services.AddSingleton<NatsJetStreamContextProvider>();
        services.AddSingleton<NatsJetStreamManager>();
        services.AddScoped<IModuleEventPublisher, NatsModuleEventPublisher>();

        if (options.ConsumerEnabled)
        {
            services.AddHostedService<NatsModuleEventConsumerBackgroundService>();
        }
    }
}
