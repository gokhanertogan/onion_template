namespace ServiceName.Application;

using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>Registers handlers, validators and event handlers of this assembly with the default pipeline.</summary>
    public static IServiceCollection AddServiceNameApplication(this IServiceCollection services) =>
        services.AddBuildingBlocksApplication(typeof(DependencyInjection).Assembly);
}
