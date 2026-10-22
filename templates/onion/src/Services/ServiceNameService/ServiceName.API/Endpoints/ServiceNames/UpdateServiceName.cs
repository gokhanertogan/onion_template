namespace ServiceName.API.Endpoints.ServiceNames;

using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Web.Endpoints;
using BuildingBlocks.Web.Results;
using BuildingBlocks.Web.Security;
using ServiceName.Application.ServiceNames.Update;
using ServiceName.Contracts.ServiceNames;

internal sealed class UpdateServiceName : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut($"{ServiceNameEndpoints.Route}/{{id:guid}}", async (
                Guid id,
                UpdateServiceNameRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new UpdateServiceNameCommand(id, request.Name, request.Description), cancellationToken);
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .WithName("UpdateServiceName")
            .WithTags(ServiceNameEndpoints.Tag)
            .WithSummary("Updates a ServiceName")
            .RequirePermission(ServiceNamePermissions.Write)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
