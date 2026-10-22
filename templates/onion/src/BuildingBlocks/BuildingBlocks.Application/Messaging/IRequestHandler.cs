namespace BuildingBlocks.Application.Messaging;

/// <summary>Handles exactly one request type. Exactly one handler per request is allowed.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
