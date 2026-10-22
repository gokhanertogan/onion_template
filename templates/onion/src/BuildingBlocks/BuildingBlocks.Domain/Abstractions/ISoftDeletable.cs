namespace BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Entities that are never physically deleted. Deletes are converted to updates
/// and a global query filter hides deleted rows.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedAtUtc { get; }

    string? DeletedBy { get; }
}
