namespace ServiceName.Persistence;

using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceName.Domain.ServiceNames;

/// <summary>Database of this service only (database-per-service). Configurations are discovered automatically.</summary>
public sealed class ServiceNameDbContext(DbContextOptions<ServiceNameDbContext> options) : AppDbContextBase(options)
{
    public DbSet<ServiceName> ServiceNames => Set<ServiceName>();
}
