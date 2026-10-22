namespace BuildingBlocks.Application.Validation;

using System.Reflection;
using BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ValidationServiceCollectionExtensions
{
    /// <summary>Registers every concrete <see cref="IValidator{T}"/> found in the assemblies (scoped, so validators may depend on repositories for async rules).</summary>
    public static IServiceCollection AddValidatorsFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var (serviceType, implementationType) in MediatorServiceCollectionExtensions.FindHandlers(assemblies, typeof(IValidator<>)))
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
        }

        return services;
    }
}
