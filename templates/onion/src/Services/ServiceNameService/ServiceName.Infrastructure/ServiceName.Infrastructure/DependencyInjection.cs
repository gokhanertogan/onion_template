namespace ServiceName.Infrastructure;

using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceNameInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBuildingBlocksInfrastructure();

        // Integration event consumers living in this assembly.
        services.AddEventHandlers(typeof(DependencyInjection).Assembly);

        // Register typed HttpClients / broker clients here, bound from `configuration`.
        return services;
    }
}
