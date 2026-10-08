namespace Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public OutboxMessageStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(string eventType, string payload)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        Status = OutboxMessageStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAtUtc = processedAtUtc;
    }

    public void RecordFailedAttempt(int maxAttempts)
    {
        Attempts++;
        Status = Attempts >= maxAttempts ? OutboxMessageStatus.Failed : OutboxMessageStatus.Pending;
    }
}
