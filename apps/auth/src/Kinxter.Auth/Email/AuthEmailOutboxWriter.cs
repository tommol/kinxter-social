using Kinxter.Auth.Infrastructure.Persistence;
using Kinxter.Shared.Abstractions.Time;

namespace Kinxter.Auth.Email;

internal sealed class AuthEmailOutboxWriter
{
    private readonly AuthDbContext dbContext;
    private readonly IClock clock;

    public AuthEmailOutboxWriter(AuthDbContext dbContext, IClock clock)
    {
        this.dbContext = dbContext;
        this.clock = clock;
    }

    public Task AddAsync(
        string recipient,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        var now = this.clock.UtcNow;
        var message = new AuthEmailMessage(
            Guid.CreateVersion7(now),
            recipient,
            subject,
            htmlBody,
            textBody,
            now);

        return this.dbContext.EmailOutboxMessages.AddAsync(message, cancellationToken).AsTask();
    }
}
