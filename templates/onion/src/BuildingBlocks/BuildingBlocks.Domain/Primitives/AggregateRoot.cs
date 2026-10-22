namespace BuildingBlocks.Domain.Primitives;

using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Events;

/// <summary>
/// Consistency boundary. Only aggregate roots are loaded/saved through repositories
/// and only they raise domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot, IHasDomainEvents, IVersioned
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>Optimistic concurrency token, regenerated on every save.</summary>
    public Guid Version { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
