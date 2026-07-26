using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Auth.Infrastructure.Outbox;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Outbox;
using Kinxter.Shared.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Auth;

internal sealed class AuthIntegrationEventPublisher
{
    private readonly IOutboxWriter<AuthOutbox>? outboxWriter;
    private readonly IModuleEventPublisher? fallbackPublisher;
    private readonly IClock clock;
    private readonly AuthOptions options;

    public AuthIntegrationEventPublisher(
        IClock clock,
        AuthOptions options,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(services);

        this.outboxWriter = services.GetService<IOutboxWriter<AuthOutbox>>();
        this.fallbackPublisher = services.GetService<IModuleEventPublisher>();
        this.clock = clock;
        this.options = options;
    }

    public Task PublishUserRegisteredAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = this.clock.UtcNow;

        var moduleEvent = new IdentityUserRegisteredV1(
                Guid.CreateVersion7(now),
                now,
                this.options.Realm,
                user.Id.ToString("D"),
                user.Email ?? user.UserName ?? "",
                user.EmailConfirmed);
        return this.outboxWriter is not null
            ? this.outboxWriter.AddAsync(moduleEvent, cancellationToken)
            : this.fallbackPublisher?.PublishAsync(moduleEvent, cancellationToken)
                ?? throw new InvalidOperationException("No auth integration event transport is registered.");
    }

    public Task PublishEmailConfirmedAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = this.clock.UtcNow;

        var moduleEvent = new IdentityEmailConfirmedV1(
                Guid.CreateVersion7(now),
                now,
                this.options.Realm,
                user.Id.ToString("D"),
                user.Email ?? user.UserName ?? "");
        return this.outboxWriter is not null
            ? this.outboxWriter.AddAsync(moduleEvent, cancellationToken)
            : this.fallbackPublisher?.PublishAsync(moduleEvent, cancellationToken)
                ?? throw new InvalidOperationException("No auth integration event transport is registered.");
    }
}
