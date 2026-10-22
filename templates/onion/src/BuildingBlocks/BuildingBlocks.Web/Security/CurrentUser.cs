namespace BuildingBlocks.Web.Security;

using System.Security.Claims;
using BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId => Principal is { Identity.IsAuthenticated: true } principal
        ? principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;

    public bool HasPermission(string permission) => Principal is { } principal && Permissions.Has(principal, permission);
}
