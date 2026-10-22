namespace ServiceName.API.Endpoints.ServiceNames;

using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Web.Endpoints;
using BuildingBlocks.Web.Results;
using BuildingBlocks.Web.Security;
using ServiceName.Application.ServiceNames.Delete;

internal sealed class DeleteServiceName : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete($"{ServiceNameEndpoints.Route}/{{id:guid}}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteServiceNameCommand(id), cancellationToken);
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .WithName("DeleteServiceName")
            .WithTags(ServiceNameEndpoints.Tag)
            .WithSummary("Deletes (soft) a ServiceName")
            .RequirePermission(ServiceNamePermissions.Write)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
