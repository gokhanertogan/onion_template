namespace ServiceName.Domain.ServiceNames;

using BuildingBlocks.Domain.Events;

public sealed record ServiceNameCreatedDomainEvent(ServiceNameId ServiceNameId, string Name) : DomainEvent;

public sealed record ServiceNameUpdatedDomainEvent(ServiceNameId ServiceNameId, string Name) : DomainEvent;

public sealed record ServiceNameArchivedDomainEvent(ServiceNameId ServiceNameId) : DomainEvent;

public sealed record ServiceNameDeletedDomainEvent(ServiceNameId ServiceNameId) : DomainEvent;
