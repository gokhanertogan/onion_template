namespace BuildingBlocks.Web.Security;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

/// <summary>
/// Permission-based authorization. A permission is granted by a "permission"/"permissions" claim
/// or by an OAuth scope ("scope"/"scp", space separated), so it works with any OIDC provider.
/// </summary>
public static class Permissions
{
    private static readonly string[] ListClaimTypes = ["permission", "permissions"];
    private static readonly string[] ScopeClaimTypes = ["scope", "scp"];

    public static bool Has(ClaimsPrincipal principal, string permission)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.Claims.Any(c =>
            (ListClaimTypes.Contains(c.Type) && string.Equals(c.Value, permission, StringComparison.Ordinal)) ||
            (ScopeClaimTypes.Contains(c.Type) && c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(permission, StringComparer.Ordinal)));
    }

    /// <summary>Requires an authenticated caller holding <paramref name="permission"/>.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission)));
}

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (Permissions.Has(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
