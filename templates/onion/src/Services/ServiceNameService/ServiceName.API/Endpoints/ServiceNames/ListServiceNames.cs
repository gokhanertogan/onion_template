namespace ServiceName.API.Endpoints.ServiceNames;

using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Web.Endpoints;
using BuildingBlocks.Web.Results;
using BuildingBlocks.Web.Security;
using ServiceName.Application.ServiceNames.List;
using ServiceName.Contracts;
using ServiceName.Contracts.ServiceNames;

internal sealed class ListServiceNames : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet(ServiceNameEndpoints.Route, async (
                string? search,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new ListServiceNamesQuery(search, page ?? 1, pageSize ?? PageRequest.DefaultPageSize);
                var result = await sender.Send(query, cancellationToken);
                return result.Match(list => Results.Ok(list.ToResponse()), ApiResults.Problem);
            })
            .WithName("ListServiceNames")
            .WithTags(ServiceNameEndpoints.Tag)
            .WithSummary("Lists ServiceNames (paged, optional name search)")
            .RequirePermission(ServiceNamePermissions.Read)
            .Produces<PagedResponse<ServiceNameResponse>>()
            .ProducesValidationProblem();
}
