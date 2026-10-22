namespace ServiceName.Application.ServiceNames.List;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Results;
using ServiceName.Contracts.ServiceNames;

public sealed record ListServiceNamesQuery(string? Search, int Page = 1, int PageSize = PageRequest.DefaultPageSize)
    : ICachedQuery<PagedList<ServiceNameResponse>>
{
    public string CacheKey => ServiceNameCacheKeys.List(Search, Page, PageSize);

    public IReadOnlyCollection<string> CacheTags => ServiceNameCacheKeys.Tags;

    public TimeSpan? Expiration => ServiceNameCacheKeys.Expiration;
}

internal sealed class ListServiceNamesQueryValidator : AbstractValidator<ListServiceNamesQuery>
{
    public ListServiceNamesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}

internal sealed class ListServiceNamesQueryHandler(IServiceNameReadRepository readRepository)
    : IQueryHandler<ListServiceNamesQuery, PagedList<ServiceNameResponse>>
{
    public async Task<Result<PagedList<ServiceNameResponse>>> Handle(ListServiceNamesQuery query, CancellationToken cancellationToken) =>
        await readRepository.ListAsync(query.Search, new PageRequest(query.Page, query.PageSize), cancellationToken);
}
