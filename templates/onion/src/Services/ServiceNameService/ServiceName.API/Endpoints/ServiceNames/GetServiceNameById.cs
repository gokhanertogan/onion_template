namespace ServiceName.API.Endpoints.ServiceNames;

using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Web.Endpoints;
using BuildingBlocks.Web.Results;
using BuildingBlocks.Web.Security;
using ServiceName.Application.ServiceNames.GetById;
using ServiceName.Contracts.ServiceNames;

internal sealed class GetServiceNameById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet($"{ServiceNameEndpoints.Route}/{{id:guid}}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetServiceNameByIdQuery(id), cancellationToken);
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .WithName(ServiceNameEndpoints.GetByIdRouteName)
            .WithTags(ServiceNameEndpoints.Tag)
            .WithSummary("Gets a ServiceName by id")
            .RequirePermission(ServiceNamePermissions.Read)
            .Produces<ServiceNameResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}
