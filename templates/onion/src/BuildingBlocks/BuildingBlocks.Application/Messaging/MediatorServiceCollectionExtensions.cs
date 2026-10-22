namespace BuildingBlocks.Application.Messaging;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class MediatorServiceCollectionExtensions
{
    /// <summary>Registers the mediator, every request handler in the configured assemblies, and the behaviors.</summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, Action<MediatorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MediatorOptions();
        configure(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<MediatorRegistry>();
        services.TryAddScoped<ISender, Mediator>();

        foreach (var behavior in options.Behaviors)
        {
            services.TryAddTransient(behavior);
        }

        foreach (var (serviceType, implementationType) in FindHandlers(options.Assemblies, typeof(IRequestHandler<,>)))
        {
            var existing = services.FirstOrDefault(d => d.ServiceType == serviceType);
            if (existing is not null && existing.ImplementationType != implementationType)
            {
                throw new InvalidOperationException(
                    $"Multiple handlers found for '{serviceType.GenericTypeArguments[0].FullName}': " +
                    $"'{existing.ImplementationType?.FullName}' and '{implementationType.FullName}'.");
            }

            services.TryAddTransient(serviceType, implementationType);
        }

        return services;
    }

    internal static IEnumerable<(Type ServiceType, Type ImplementationType)> FindHandlers(
        IEnumerable<Assembly> assemblies,
        Type openHandlerInterface) =>
        from type in assemblies.SelectMany(GetLoadableTypes)
        where type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
        from @interface in type.GetInterfaces()
        where @interface.IsGenericType && @interface.GetGenericTypeDefinition() == openHandlerInterface
        select (@interface, type);

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
