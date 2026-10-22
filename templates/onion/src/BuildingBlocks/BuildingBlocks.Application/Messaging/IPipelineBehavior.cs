namespace BuildingBlocks.Application.Messaging;

/// <summary>Continuation to the next behavior (or the handler) in the pipeline.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);

/// <summary>
/// Cross-cutting concern wrapped around request handlers (logging, validation, caching, transactions...).
/// Behaviors are open generic types; generic constraints decide which requests they apply to.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
