namespace ServiceName.Persistence;

using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceName.Application.ServiceNames;
using ServiceName.Domain.ServiceNames;
using ServiceName.Persistence.Repositories;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ServiceNameDb";

    public static IServiceCollection AddServiceNamePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAppDbContext<ServiceNameDbContext>(configuration, (serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

#if (UsePostgres)
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
#elif (UseSqlite)
            options.UseSqlite(connectionString);
#else
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
#endif

            if (serviceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IServiceNameRepository, ServiceNameRepository>();
        services.AddScoped<IServiceNameReadRepository, ServiceNameReadRepository>();

        // "ready" == BuildingBlocks.Web.Health.HealthCheckTags.Ready (readiness probe).
        services.AddHealthChecks().AddDbContextCheck<ServiceNameDbContext>("database", tags: ["ready"]);

        return services;
    }
}
