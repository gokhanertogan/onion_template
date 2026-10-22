namespace ServiceName.API.Endpoints.ServiceNames;

using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Web.Endpoints;
using BuildingBlocks.Web.Results;
using BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Mvc;
using ServiceName.Application.ServiceNames.Create;
using ServiceName.Contracts.ServiceNames;

internal sealed class CreateServiceName : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost(ServiceNameEndpoints.Route, async (
                CreateServiceNameRequest request,
                [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateServiceNameCommand(request.Name, request.Description, idempotencyKey);
                var result = await sender.Send(command, cancellationToken);

                return result.Match(
                    id => Results.CreatedAtRoute(ServiceNameEndpoints.GetByIdRouteName, new { id }, new CreatedResponse(id)),
                    ApiResults.Problem);
            })
            .WithName("CreateServiceName")
            .WithTags(ServiceNameEndpoints.Tag)
            .WithSummary("Creates a ServiceName")
            .WithDescription("Send an Idempotency-Key header to make retries safe.")
            .RequirePermission(ServiceNamePermissions.Write)
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
}
