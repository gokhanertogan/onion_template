namespace BuildingBlocks.Application.Events;

/// <summary>
/// Publishes integration events to other services. The default implementation is in-process;
/// swap it for a broker (RabbitMQ, Kafka, Azure Service Bus...) without touching the application layer.
/// </summary>
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}

/// <summary>Consumes an integration event. Delivery is at-least-once: make handlers idempotent.</summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class
{
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);
}
