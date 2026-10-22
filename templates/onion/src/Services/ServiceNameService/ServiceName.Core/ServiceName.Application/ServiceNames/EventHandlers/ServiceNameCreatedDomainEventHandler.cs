namespace ServiceName.Application.ServiceNames.EventHandlers;

using BuildingBlocks.Application.Events;
using ServiceName.Contracts.ServiceNames;
using ServiceName.Domain.ServiceNames;

/// <summary>
/// Runs after the creating transaction committed (delivered by the outbox) and
/// notifies other services through the event bus.
/// </summary>
internal sealed class ServiceNameCreatedDomainEventHandler(IEventBus eventBus) : IDomainEventHandler<ServiceNameCreatedDomainEvent>
{
    public async Task Handle(ServiceNameCreatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        await eventBus.PublishAsync(
            new ServiceNameCreatedIntegrationEvent(
                domainEvent.EventId,
                domainEvent.OccurredOnUtc,
                domainEvent.ServiceNameId.Value,
                domainEvent.Name),
            cancellationToken);
}
