namespace BuildingBlocks.Application.Messaging;

using System.Reflection;

/// <summary>Configures handler discovery and the ordered behavior pipeline.</summary>
public sealed class MediatorOptions
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<Type> _behaviors = [];

    public IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>Open generic behavior types, outermost first.</summary>
    public IReadOnlyList<Type> Behaviors => _behaviors;

    public MediatorOptions RegisterHandlersFromAssemblies(params Assembly[] assemblies)
    {
        _assemblies.AddRange(assemblies.Except(_assemblies));
        return this;
    }

    /// <summary>Appends an open generic behavior. Registration order == execution order (outermost first).</summary>
    public MediatorOptions AddOpenBehavior(Type openBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        var implementsBehavior = openBehaviorType.IsGenericTypeDefinition &&
            openBehaviorType.GetGenericArguments().Length == 2 &&
            openBehaviorType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!implementsBehavior)
        {
            throw new ArgumentException(
                $"{openBehaviorType.Name} must be an open generic type with two type parameters implementing IPipelineBehavior<,>.",
                nameof(openBehaviorType));
        }

        if (!_behaviors.Contains(openBehaviorType))
        {
            _behaviors.Add(openBehaviorType);
        }

        return this;
    }
}
