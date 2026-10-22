namespace BuildingBlocks.Infrastructure.Persistence.Interceptors;

using System.Text.Json;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Transactional outbox: converts domain events raised by aggregates into outbox rows that are
/// written in the same transaction as the state change. A background processor publishes them.
/// </summary>
public sealed class OutboxInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            var aggregates = context.ChangeTracker.Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            var now = timeProvider.GetUtcNow();
            var messages = aggregates
                .SelectMany(a => a.DomainEvents)
                .Select(domainEvent => new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    Type = OutboxMessage.GetTypeName(domainEvent.GetType()),
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), OutboxMessage.SerializerOptions),
                    OccurredOnUtc = now,
                })
                .ToList();

            aggregates.ForEach(a => a.ClearDomainEvents());
            context.Set<OutboxMessage>().AddRange(messages);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
