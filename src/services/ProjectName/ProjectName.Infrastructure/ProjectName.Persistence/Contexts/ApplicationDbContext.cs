using Microsoft.EntityFrameworkCore;
using ProjectName.Persistence.Contexts.Configurations;

namespace ProjectName.Persistence.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.ProjectName> ProjectNames { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProjectNameEntityTypeConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}