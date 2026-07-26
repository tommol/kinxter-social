namespace Kinxter.Auth.Infrastructure.Persistence;

public sealed class AuthEmailMessage
{
    private AuthEmailMessage()
    {
        Recipient = null!;
        Subject = null!;
        HtmlBody = null!;
        TextBody = null!;
    }

    public AuthEmailMessage(
        Guid id,
        string recipient,
        string subject,
        string htmlBody,
        string textBody,
        DateTimeOffset createdAt)
    {
        Id = id;
        Recipient = recipient;
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Recipient { get; private set; }

    public string Subject { get; private set; }

    public string HtmlBody { get; private set; }

    public string TextBody { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? LastAttemptedAt { get; private set; }

    public int RetryCount { get; private set; }

    public string? Error { get; private set; }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        LastAttemptedAt = processedAt;
        Error = null;
    }

    public void MarkFailed(DateTimeOffset attemptedAt, string error)
    {
        LastAttemptedAt = attemptedAt;
        RetryCount++;
        Error = error.Length <= 2000 ? error : error[..2000];
    }
}
