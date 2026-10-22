namespace BuildingBlocks.Application.Events;

using BuildingBlocks.Domain.Events;

/// <summary>
/// Reacts to a domain event. Events are delivered from the transactional outbox
/// (at-least-once), so handlers must be idempotent.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
