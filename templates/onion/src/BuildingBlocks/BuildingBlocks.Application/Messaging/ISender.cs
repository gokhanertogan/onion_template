namespace BuildingBlocks.Application.Messaging;

/// <summary>Sends a request through the pipeline to its single handler.</summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
