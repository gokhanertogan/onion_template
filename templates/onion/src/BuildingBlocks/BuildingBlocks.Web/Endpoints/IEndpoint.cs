namespace BuildingBlocks.Web.Endpoints;

using Microsoft.AspNetCore.Routing;

/// <summary>One endpoint (vertical slice). Implementations are discovered and mapped automatically.</summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
