using System.Net;
using System.Net.Mail;
using Kinxter.Auth.Infrastructure.Persistence;

namespace Kinxter.Auth.Email;

internal sealed class SmtpAuthEmailSender : IAuthEmailSender
{
    private readonly AuthEmailOptions options;

    public SmtpAuthEmailSender(AuthEmailOptions options) => this.options = options;

    public async Task SendAsync(AuthEmailMessage message, CancellationToken cancellationToken = default)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(this.options.FromAddress, this.options.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false
        };
        mail.To.Add(message.Recipient);
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html"));

        using var client = new SmtpClient(this.options.Host, this.options.Port)
        {
            EnableSsl = this.options.UseTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(this.options.Username),
            Credentials = string.IsNullOrWhiteSpace(this.options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(this.options.Username, this.options.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(mail, cancellationToken);
    }
}
