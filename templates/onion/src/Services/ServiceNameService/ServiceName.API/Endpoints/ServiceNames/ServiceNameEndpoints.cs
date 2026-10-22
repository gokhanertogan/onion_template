namespace ServiceName.API.Endpoints.ServiceNames;

using BuildingBlocks.Application.Pagination;
using ServiceName.Contracts;

internal static class ServiceNameEndpoints
{
    public const string Route = "service-names";
    public const string Tag = "ServiceNames";
    public const string GetByIdRouteName = "GetServiceNameById";

    public static PagedResponse<T> ToResponse<T>(this PagedList<T> page) =>
        new(page.Items, page.Page, page.PageSize, page.TotalCount, page.TotalPages, page.HasNextPage, page.HasPreviousPage);
}

/// <summary>
/// Permissions of this service. Grant them as "permission" claims or OAuth scopes.
/// Locally: dotnet user-jwts create --scope "service-names:read" --scope "service-names:write"
/// </summary>
public static class ServiceNamePermissions
{
    public const string Read = "service-names:read";
    public const string Write = "service-names:write";
}
