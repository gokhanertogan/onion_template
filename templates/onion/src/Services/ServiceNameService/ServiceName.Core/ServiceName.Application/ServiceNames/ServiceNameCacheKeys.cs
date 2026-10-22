namespace ServiceName.Application.ServiceNames;

using System.Globalization;

/// <summary>Cache keys and tags. Every entry is tagged so any write invalidates them together.</summary>
internal static class ServiceNameCacheKeys
{
    public const string Tag = "service-names";

    public static readonly IReadOnlyCollection<string> Tags = [Tag];

    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    public static string ById(Guid id) => $"service-names:{id:N}";

    public static string List(string? search, int page, int pageSize) =>
        string.Create(CultureInfo.InvariantCulture, $"service-names:list:{search?.Trim().ToUpperInvariant()}:{page}:{pageSize}");
}
