using Kinxter.Auth.Infrastructure.Persistence;

namespace Kinxter.Auth.Email;

internal interface IAuthEmailSender
{
    Task SendAsync(AuthEmailMessage message, CancellationToken cancellationToken = default);
}
