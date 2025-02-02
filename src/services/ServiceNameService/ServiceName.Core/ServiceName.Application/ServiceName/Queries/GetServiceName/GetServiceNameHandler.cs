using BuildingBlocks.CQRS;
using BuildingBlocks.Results;
using Mapster;
using ServiceName.Application.ServiceName.Common;
using ServiceName.Application.ServiceName.Repositories;

namespace ServiceName.Application.ServiceName.Queries.GetServiceName;
public class GetServiceNameHandler(IServiceNameReadRepository serviceNameReadRepository) : IQueryHandler<GetServiceNameQuery, PaginatedResult<ServiceNameResult>>
{
    private readonly IServiceNameReadRepository _serviceNameReadRepository = serviceNameReadRepository;

    public async Task<PaginatedResult<ServiceNameResult>> Handle(GetServiceNameQuery request, CancellationToken cancellationToken)
    {
        var totalItemCount = await _serviceNameReadRepository.CountAsync(cancellationToken, x => x.Name == request.Name);
        var items = await _serviceNameReadRepository.GetWherePaginatedAsync(x => x.Name == request.Name,
                                                                            request.PaginationRequest.PageIndex,
                                                                            request.PaginationRequest.PageSize,
                                                                            cancellationToken);

        return PaginatedResult<ServiceNameResult>.Success(data: items.Adapt<List<ServiceNameResult>>(),
                                                          totalCount: totalItemCount,
                                                          pageNumber: request.PaginationRequest.PageIndex,
                                                          pageSize: request.PaginationRequest.PageSize,
                                                          statusCode: 200);
    }
}