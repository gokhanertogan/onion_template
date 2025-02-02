using Microsoft.EntityFrameworkCore;
using ServiceName.Persistence.Contexts.Configurations;

namespace ServiceName.Persistence.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.ServiceName> ServiceNames { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ServiceNameEntityTypeConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}