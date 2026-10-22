namespace BuildingBlocks.Application.Abstractions;

/// <summary>
/// Commits all changes tracked during a request atomically (including outbox messages).
/// Called by <c>UnitOfWorkBehavior</c> after successful commands; handlers do not call it.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
