namespace Kinxter.Accounts.Model;

public sealed class ProcessedAccountEvent
{
    private ProcessedAccountEvent()
    {
        EventType = null!;
    }

    public ProcessedAccountEvent(Guid eventId, string eventType, DateTimeOffset processedAt)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }

    public Guid EventId { get; private set; }

    public string EventType { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }
}
