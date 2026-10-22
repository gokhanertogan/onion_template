namespace BuildingBlocks.Application;

using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.Events;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the mediator with the default pipeline (outermost first):
    /// Logging → Validation → Idempotency → Caching → CacheInvalidation → UnitOfWork → Handler.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksApplication(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediator(options => options
            .RegisterHandlersFromAssemblies(assemblies)
            .AddOpenBehavior(typeof(LoggingBehavior<,>))
            .AddOpenBehavior(typeof(ValidationBehavior<,>))
            .AddOpenBehavior(typeof(IdempotencyBehavior<,>))
            .AddOpenBehavior(typeof(CachingBehavior<,>))
            .AddOpenBehavior(typeof(CacheInvalidationBehavior<,>))
            .AddOpenBehavior(typeof(UnitOfWorkBehavior<,>)));

        services.AddValidatorsFromAssemblies(assemblies);

        services.AddEventHandlers(assemblies);

        return services;
    }

    /// <summary>Registers domain and integration event handlers (e.g. consumers living in the infrastructure layer).</summary>
    public static IServiceCollection AddEventHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        foreach (var openInterface in new[] { typeof(IDomainEventHandler<>), typeof(IIntegrationEventHandler<>) })
        {
            foreach (var (serviceType, implementationType) in MediatorServiceCollectionExtensions.FindHandlers(assemblies, openInterface))
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
            }
        }

        return services;
    }
}
