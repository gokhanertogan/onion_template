namespace BuildingBlocks.Infrastructure;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Events;
using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DependencyInjection
{
    /// <summary>Time, caching and messaging infrastructure shared by every service.</summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Register an IDistributedCache (e.g. Redis) before this call to enable the L2 cache automatically.
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 512;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
            };
        });
        services.TryAddSingleton<ICacheService, HybridCacheService>();

        services.TryAddSingleton<InMemoryEventBus>();
        services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
        services.AddHostedService<InMemoryEventBusWorker>();

        return services;
    }
}
