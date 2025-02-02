using BuildingBlocks.Results;
using FastEndpoints;
using MediatR;
using ServiceName.Application.ServiceName.Common;
using ServiceName.Application.ServiceName.Queries.GetServiceName;
using ServiceName.Contracts.Requests;

namespace ServiceName.API.Endpoints.ServiceName;

public class GetServiceName(IMediator _mediator)
 : Endpoint<GetServiceNameRequest, PaginatedResult<ServiceNameResult>>
{
    public override void Configure()
    {
        Get(BaseRequest.Route);
    }

    public override async Task HandleAsync(
    GetServiceNameRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetServiceNameQuery(
            request.Name!,
            new() { PageIndex = request.PageIndex, PageSize = request.PageSize }), cancellationToken);

        Response = result;
        return;
    }
}