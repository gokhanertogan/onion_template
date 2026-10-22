namespace ServiceName.Contracts.ServiceNames;

/// <summary>Published to other services when a ServiceName is created. Versioned by type name; never break it.</summary>
public sealed record ServiceNameCreatedIntegrationEvent(Guid EventId, DateTimeOffset OccurredOnUtc, Guid ServiceNameId, string Name);
