using Microsoft.Extensions.DependencyInjection;
using ProjectName.Application.Common.Mapping;

namespace ProjectName.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        MappingConfigurations.RegisterMappings();
        return services;
    }
}