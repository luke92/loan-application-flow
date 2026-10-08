namespace Infrastructure.Outbox;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "Outbox";

    public int PollingIntervalSeconds { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
}
