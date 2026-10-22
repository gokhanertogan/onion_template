namespace ServiceName.Infrastructure.IntegrationEvents;

using BuildingBlocks.Application.Events;
using Microsoft.Extensions.Logging;
using ServiceName.Contracts.ServiceNames;

/// <summary>
/// Example consumer. In a real system this lives in another service and the event arrives
/// through a broker; with the in-memory bus it runs in-process.
/// </summary>
internal sealed partial class ServiceNameCreatedIntegrationEventHandler(ILogger<ServiceNameCreatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<ServiceNameCreatedIntegrationEvent>
{
    public Task Handle(ServiceNameCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        LogReceived(logger, integrationEvent.ServiceNameId, integrationEvent.EventId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 5000, Level = LogLevel.Information, Message = "ServiceName {ServiceNameId} created (event {EventId})")]
    private static partial void LogReceived(ILogger logger, Guid serviceNameId, Guid eventId);
}
