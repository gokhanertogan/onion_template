namespace BuildingBlocks.Infrastructure.Persistence;

using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

/// <summary>EF Core implementation of the aggregate repository. Never calls SaveChanges.</summary>
public abstract class Repository<TAggregate, TId>(DbContext dbContext) : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    protected DbContext DbContext { get; } = dbContext;

    protected DbSet<TAggregate> Set => DbContext.Set<TAggregate>();

    public virtual async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public void Add(TAggregate aggregate) => Set.Add(aggregate);

    public void Remove(TAggregate aggregate) => Set.Remove(aggregate);
}
