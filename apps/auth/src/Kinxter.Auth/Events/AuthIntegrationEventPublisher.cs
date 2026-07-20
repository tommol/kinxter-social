using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.IntegrationEvents.Identity;
using Kinxter.Shared.Abstractions.Events;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Auth;

internal sealed class AuthIntegrationEventPublisher
{
    private readonly IModuleEventPublisher publisher;
    private readonly IClock clock;
    private readonly AuthOptions options;

    public AuthIntegrationEventPublisher(
        IModuleEventPublisher publisher,
        IClock clock,
        AuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);

        this.publisher = publisher;
        this.clock = clock;
        this.options = options;
    }

    public Task PublishUserRegisteredAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = this.clock.UtcNow;

        return this.publisher.PublishAsync(
            new IdentityUserRegisteredV1(
                Guid.CreateVersion7(now),
                now,
                this.options.Realm,
                user.Id.ToString("D"),
                user.Email ?? user.UserName ?? "",
                user.EmailConfirmed),
            cancellationToken);
    }

    public Task PublishEmailConfirmedAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = this.clock.UtcNow;

        return this.publisher.PublishAsync(
            new IdentityEmailConfirmedV1(
                Guid.CreateVersion7(now),
                now,
                this.options.Realm,
                user.Id.ToString("D"),
                user.Email ?? user.UserName ?? ""),
            cancellationToken);
    }
}
