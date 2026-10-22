namespace BuildingBlocks.Application.Messaging;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency-free mediator. The pipeline for each request type is built once and cached,
/// so steady-state dispatch costs one dictionary lookup plus the delegate chain.
/// </summary>
internal sealed class Mediator(IServiceProvider serviceProvider, MediatorRegistry registry) : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = (RequestHandlerWrapper<TResponse>)registry.GetWrapper(request.GetType(), typeof(TResponse));
        return handler.Handle(request, serviceProvider, cancellationToken);
    }
}

/// <summary>Per-container cache of compiled request pipelines.</summary>
internal sealed class MediatorRegistry(MediatorOptions options)
{
    private readonly ConcurrentDictionary<Type, RequestHandlerWrapperBase> _wrappers = new();

    public RequestHandlerWrapperBase GetWrapper(Type requestType, Type responseType) =>
        _wrappers.GetOrAdd(requestType, CreateWrapper, (responseType, options));

    private static RequestHandlerWrapperBase CreateWrapper(Type requestType, (Type ResponseType, MediatorOptions Options) state)
    {
        var behaviorTypes = state.Options.Behaviors
            .Select(open => TryClose(open, requestType, state.ResponseType))
            .OfType<Type>()
            .ToArray();

        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, state.ResponseType);
        return (RequestHandlerWrapperBase)Activator.CreateInstance(wrapperType, [behaviorTypes])!;
    }

    /// <summary>Closes an open behavior; returns null when the generic constraints do not match the request.</summary>
    private static Type? TryClose(Type openBehavior, Type requestType, Type responseType)
    {
        try
        {
            return openBehavior.MakeGenericType(requestType, responseType);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

#pragma warning disable CA1040
internal abstract class RequestHandlerWrapperBase;
#pragma warning restore CA1040

internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerWrapperBase
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse>(Type[] behaviorTypes) : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException($"No handler registered for request '{typeof(TRequest).FullName}'.");

        RequestHandlerDelegate<TResponse> next = ct => handler.Handle(typedRequest, ct);

        for (var i = behaviorTypes.Length - 1; i >= 0; i--)
        {
            var behavior = (IPipelineBehavior<TRequest, TResponse>)serviceProvider.GetRequiredService(behaviorTypes[i]);
            var continuation = next;
            next = ct => behavior.Handle(typedRequest, continuation, ct);
        }

        return next(cancellationToken);
    }
}
