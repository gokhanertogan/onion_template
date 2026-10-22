namespace BuildingBlocks.Application.Behaviors;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;

/// <summary>Invalidates cache tags after a command has been committed successfully.</summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(ICacheService cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidator
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        if (response.IsSuccess)
        {
            foreach (var tag in request.CacheTagsToInvalidate)
            {
                await cache.RemoveByTagAsync(tag, CancellationToken.None);
            }
        }

        return response;
    }
}
