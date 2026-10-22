namespace BuildingBlocks.Infrastructure.Caching;

using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// <see cref="ICacheService"/> over <see cref="HybridCache"/>: in-memory L1, optional distributed L2
/// (any registered IDistributedCache), stampede protection and tag-based invalidation.
/// </summary>
internal sealed class HybridCacheService(HybridCache cache) : ICacheService
{
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = expiration is { } ttl
            ? new HybridCacheEntryOptions { Expiration = ttl, LocalCacheExpiration = ttl }
            : null;

        return cache.GetOrCreateAsync(key, factory, static (f, ct) => f(ct), options, tags, cancellationToken);
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        cache.RemoveByTagAsync(tag, cancellationToken);
}
