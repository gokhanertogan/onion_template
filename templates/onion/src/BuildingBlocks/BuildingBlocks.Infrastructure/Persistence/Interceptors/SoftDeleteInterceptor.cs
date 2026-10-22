namespace BuildingBlocks.Infrastructure.Persistence.Interceptors;

using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>Turns deletes of <see cref="ISoftDeletable"/> entities into updates.</summary>
public sealed class SoftDeleteInterceptor(TimeProvider timeProvider, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>().Where(e => e.State == EntityState.Deleted))
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(ISoftDeletable.DeletedAtUtc)).CurrentValue = timeProvider.GetUtcNow();
                entry.Property(nameof(ISoftDeletable.DeletedBy)).CurrentValue = currentUser.UserId;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
