namespace BuildingBlocks.Infrastructure.Persistence;

using System.Linq.Expressions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Base DbContext for every service. Applies entity configurations from the derived context's
/// assembly and the cross-cutting conventions: soft-delete filter, concurrency token, outbox table.
/// </summary>
public abstract class AppDbContextBase(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("The resource was modified by another request. Reload it and try again.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(t => t.BaseType is null))
        {
            var clrType = entityType.ClrType;

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var filter = Expression.Lambda(
                    Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted))),
                    parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
                modelBuilder.Entity(clrType).HasIndex(nameof(ISoftDeletable.IsDeleted));
            }

            if (typeof(IVersioned).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property(nameof(IVersioned.Version)).IsConcurrencyToken();
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Sensible defaults: bounded strings and fixed-precision decimals unless configured otherwise.
        configurationBuilder.Properties<string>().HaveMaxLength(512);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }
}
