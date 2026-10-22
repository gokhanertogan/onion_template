namespace BuildingBlocks.Infrastructure.Persistence;

using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
using BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TDbContext"/> with the building-block interceptors, the unit of work,
    /// the outbox processor and the startup database initializer. <paramref name="configureProvider"/>
    /// selects the database provider (UseSqlServer, UseNpgsql, UseSqlite...).
    /// </summary>
    public static IServiceCollection AddAppDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IServiceProvider, DbContextOptionsBuilder> configureProvider)
        where TDbContext : AppDbContextBase
    {
        ArgumentNullException.ThrowIfNull(configureProvider);

        services.TryAddSingleton(TimeProvider.System);

        // Order matters: soft delete converts deletes to updates before auditing and versioning run.
        services.TryAddScoped<SoftDeleteInterceptor>();
        services.TryAddScoped<AuditingInterceptor>();
        services.TryAddSingleton<VersioningInterceptor>();
        services.TryAddScoped<OutboxInterceptor>();

        services.AddDbContext<TDbContext>((sp, options) =>
        {
            configureProvider(sp, options);
            options.AddInterceptors(GetInterceptors(sp));
        });

        services.TryAddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TDbContext>());

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddHostedService<DatabaseInitializer<TDbContext>>();
        services.AddHostedService<OutboxProcessor<TDbContext>>();

        return services;
    }

    /// <summary>Building-block interceptors in execution order. Reuse when re-registering a DbContext (e.g. in tests).</summary>
    public static IInterceptor[] GetInterceptors(IServiceProvider serviceProvider) =>
    [
        serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
        serviceProvider.GetRequiredService<AuditingInterceptor>(),
        serviceProvider.GetRequiredService<VersioningInterceptor>(),
        serviceProvider.GetRequiredService<OutboxInterceptor>(),
    ];
}
