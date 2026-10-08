namespace Infrastructure.Outbox;

public enum OutboxMessageStatus
{
    Pending,
    Processed,
    Failed
}
