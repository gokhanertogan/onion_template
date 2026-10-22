namespace BuildingBlocks.Application.Caching;

using BuildingBlocks.Application.Messaging;

/// <summary>Queries implementing this are served from cache by <c>CachingBehavior</c>. Only successful results are cached.</summary>
public interface ICachedQuery
{
    string CacheKey { get; }

    /// <summary>Tags allow invalidating groups of entries (e.g. every page of a list).</summary>
    IReadOnlyCollection<string> CacheTags { get; }

    TimeSpan? Expiration { get; }
}

public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery;

/// <summary>Commands implementing this invalidate the listed tags after a successful commit.</summary>
public interface ICacheInvalidator
{
    IReadOnlyCollection<string> CacheTagsToInvalidate { get; }
}
