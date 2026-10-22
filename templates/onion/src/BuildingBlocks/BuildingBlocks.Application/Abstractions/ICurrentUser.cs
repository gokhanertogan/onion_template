namespace BuildingBlocks.Application.Abstractions;

/// <summary>The authenticated caller. Implemented by the web layer.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Subject identifier ("sub" claim) or null for anonymous callers.</summary>
    string? UserId { get; }

    bool HasPermission(string permission);
}
