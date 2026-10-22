namespace BuildingBlocks.Infrastructure.Persistence.Interceptors;

using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Regenerates the <see cref="IVersioned.Version"/> token on every insert/update. The token is a
/// concurrency token, so concurrent writers get a <c>DbUpdateConcurrencyException</c>.
/// Works on every relational provider (no rowversion/xmin required).
/// </summary>
public sealed class VersioningInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            foreach (var entry in context.ChangeTracker.Entries<IVersioned>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Property(nameof(IVersioned.Version)).CurrentValue = Guid.CreateVersion7();
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
