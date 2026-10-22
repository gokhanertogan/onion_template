namespace BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Repositories exist per aggregate root only. Persistence is committed by the
/// unit of work (see the application pipeline), never by the repository.
/// </summary>
public interface IRepository<TAggregate, in TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);

    void Remove(TAggregate aggregate);
}
