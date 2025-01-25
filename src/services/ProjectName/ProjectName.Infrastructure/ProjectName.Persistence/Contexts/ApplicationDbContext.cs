using Microsoft.EntityFrameworkCore;

namespace ProjectName.Persistence.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.ProjectName> ProjectNames { get; set; } = default!;
}