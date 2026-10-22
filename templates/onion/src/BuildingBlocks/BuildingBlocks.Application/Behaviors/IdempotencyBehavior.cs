namespace BuildingBlocks.Application.Behaviors;

using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;

/// <summary>
/// Executes an <see cref="IIdempotentCommand"/> at most once per (user, command, key) and replays the
/// first successful result for retries. Concurrent duplicates are coalesced by the cache.
/// Use a distributed L2 cache in multi-instance deployments.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(
    ICacheService cache,
    ICurrentUser currentUser,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
    where TResponse : Result
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is not { } key)
        {
            return await next(cancellationToken);
        }

        var executed = false;
        var cacheKey = $"idempotency:{typeof(TRequest).FullName}:{currentUser.UserId ?? "anonymous"}:{key:N}";
        try
        {
            var response = await cache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    executed = true;
                    var result = await next(ct);
                    return result.IsSuccess ? result : throw new UncacheableResultException(result);
                },
                Retention,
                cancellationToken: cancellationToken);

            if (!executed)
            {
                BehaviorLog.IdempotentReplay(logger, typeof(TRequest).Name, key);
            }

            return response;
        }
        catch (UncacheableResultException ex)
        {
            return (TResponse)ex.Result;
        }
    }
}
