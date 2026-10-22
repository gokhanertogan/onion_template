namespace ServiceName.Application.ServiceNames;

using BuildingBlocks.Application.Pagination;
using ServiceName.Contracts.ServiceNames;
using ServiceName.Domain.ServiceNames;

/// <summary>
/// Read side (CQRS): queries project straight to response models with no tracking,
/// bypassing the aggregate repository.
/// </summary>
public interface IServiceNameReadRepository
{
    Task<ServiceNameResponse?> GetByIdAsync(ServiceNameId id, CancellationToken cancellationToken = default);

    Task<PagedList<ServiceNameResponse>> ListAsync(string? search, PageRequest page, CancellationToken cancellationToken = default);
}
