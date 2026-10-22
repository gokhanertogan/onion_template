namespace BuildingBlocks.Infrastructure.Persistence.Outbox;

using System.ComponentModel.DataAnnotations;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; } = true;

    [Range(typeof(TimeSpan), "00:00:00.100", "01:00:00")]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 50;

    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 5;
}
