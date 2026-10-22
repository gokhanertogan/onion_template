namespace ServiceName.API.Endpoints.ServiceNames;

using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Web.Endpoints;
using BuildingBlocks.Web.Results;
using BuildingBlocks.Web.Security;
using ServiceName.Application.ServiceNames.Archive;

internal sealed class ArchiveServiceName : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost($"{ServiceNameEndpoints.Route}/{{id:guid}}/archive", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ArchiveServiceNameCommand(id), cancellationToken);
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .WithName("ArchiveServiceName")
            .WithTags(ServiceNameEndpoints.Tag)
            .WithSummary("Archives a ServiceName")
            .RequirePermission(ServiceNamePermissions.Write)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
