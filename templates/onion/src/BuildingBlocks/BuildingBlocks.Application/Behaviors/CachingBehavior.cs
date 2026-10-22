namespace BuildingBlocks.Application.Behaviors;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;

/// <summary>
/// Serves <see cref="ICachedQuery"/> requests from cache with stampede protection.
/// Failure results are never cached.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetOrCreateAsync(
                request.CacheKey,
                async ct =>
                {
                    var response = await next(ct);
                    return response.IsSuccess ? response : throw new UncacheableResultException(response);
                },
                request.Expiration,
                request.CacheTags,
                cancellationToken);
        }
        catch (UncacheableResultException ex)
        {
            return (TResponse)ex.Result;
        }
    }
}

/// <summary>Carries a failure result out of the cache factory so it is not stored.</summary>
internal sealed class UncacheableResultException(Result result) : Exception("Result is a failure and must not be cached.")
{
    public Result Result { get; } = result;
}
