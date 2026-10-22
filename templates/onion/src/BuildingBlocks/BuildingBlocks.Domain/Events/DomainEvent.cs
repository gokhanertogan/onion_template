namespace BuildingBlocks.Domain.Events;

/// <summary>Convenience base record for domain events.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
