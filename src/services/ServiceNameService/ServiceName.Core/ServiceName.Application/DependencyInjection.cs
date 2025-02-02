using System.Reflection;
using BuildingBlocks.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using ServiceName.Application.Common.Mapping;

namespace ServiceName.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
       {
           config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
           config.AddOpenBehavior(typeof(ValidationBehavior<,>));
           config.AddOpenBehavior(typeof(LoggingBehavior<,>));
       });

        services.AddFeatureManagement();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        MappingConfigurations.RegisterMappings();
        return services;
    }
}