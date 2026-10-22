namespace ServiceName.Application.ServiceNames.GetById;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using ServiceName.Contracts.ServiceNames;
using ServiceName.Domain.ServiceNames;

public sealed record GetServiceNameByIdQuery(Guid Id) : ICachedQuery<ServiceNameResponse>
{
    public string CacheKey => ServiceNameCacheKeys.ById(Id);

    public IReadOnlyCollection<string> CacheTags => ServiceNameCacheKeys.Tags;

    public TimeSpan? Expiration => ServiceNameCacheKeys.Expiration;
}

internal sealed class GetServiceNameByIdQueryHandler(IServiceNameReadRepository readRepository)
    : IQueryHandler<GetServiceNameByIdQuery, ServiceNameResponse>
{
    public async Task<Result<ServiceNameResponse>> Handle(GetServiceNameByIdQuery query, CancellationToken cancellationToken)
    {
        var id = new ServiceNameId(query.Id);
        var response = await readRepository.GetByIdAsync(id, cancellationToken);

        return response is null ? ServiceNameErrors.NotFound(id) : response;
    }
}
