namespace BuildingBlocks.Domain.Events;

/// <summary>Something meaningful that happened inside the domain.</summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}
