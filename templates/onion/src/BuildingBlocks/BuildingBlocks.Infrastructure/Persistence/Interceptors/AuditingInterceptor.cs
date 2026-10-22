namespace BuildingBlocks.Infrastructure.Persistence.Interceptors;

using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>Stamps <see cref="IAuditable"/> entities with who/when on insert and update.</summary>
public sealed class AuditingInterceptor(TimeProvider timeProvider, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            var now = timeProvider.GetUtcNow();
            var user = currentUser.UserId;

            foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).CurrentValue = now;
                    entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue = user;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(IAuditable.ModifiedAtUtc)).CurrentValue = now;
                    entry.Property(nameof(IAuditable.ModifiedBy)).CurrentValue = user;
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
