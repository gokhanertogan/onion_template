namespace BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Audit columns populated automatically by the persistence layer.
/// Setters are intentionally not exposed; EF Core writes the backing properties.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }

    string? CreatedBy { get; }

    DateTimeOffset? ModifiedAtUtc { get; }

    string? ModifiedBy { get; }
}
