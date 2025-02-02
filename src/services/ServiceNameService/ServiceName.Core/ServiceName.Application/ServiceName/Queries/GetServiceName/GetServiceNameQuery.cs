using BuildingBlocks.CQRS;
using BuildingBlocks.Results;
using ServiceName.Application.ServiceName.Common;

namespace ServiceName.Application.ServiceName.Queries.GetServiceName;

public record GetServiceNameQuery(string Name, PaginationRequest PaginationRequest) 
    : IQuery<PaginatedResult<ServiceNameResult>>;