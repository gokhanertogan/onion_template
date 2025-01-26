using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Application.ProjectName.Repositories;
using ProjectName.Persistence.Contexts;
using ProjectName.Persistence.Repositories.ProjectName;

namespace ProjectName.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        return services;
    }

    public static IServiceCollection AddPersistence(
       this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
          options.UseNpgsql("Host=localhost;Port=5432;Database=RealEstateDb;Username=sa;Password=Ge12345*"));
        // services.AddDbContext<ApplicationDbContext>(options =>
        //                     options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")));

        services.AddScoped<IProjectNameReadRepository, ProjectNameReadRepository>();
        services.AddScoped<IProjectNameWriteRepository, ProjectNameWriteRepository>();

        return services;
    }
}