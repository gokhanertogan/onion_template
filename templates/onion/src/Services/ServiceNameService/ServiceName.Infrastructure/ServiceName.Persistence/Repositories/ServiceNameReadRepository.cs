namespace ServiceName.Persistence.Repositories;

using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceName.Application.ServiceNames;
using ServiceName.Contracts.ServiceNames;
using ServiceName.Domain.ServiceNames;

internal sealed class ServiceNameReadRepository(ServiceNameDbContext dbContext) : IServiceNameReadRepository
{
    public async Task<ServiceNameResponse?> GetByIdAsync(ServiceNameId id, CancellationToken cancellationToken = default)
    {
        var row = await Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row is null ? null : ToResponse(row);
    }

    public async Task<PagedList<ServiceNameResponse>> ListAsync(string? search, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
#pragma warning disable CA1304, CA1311, CA1862 // Translated to SQL UPPER(); culture overloads are not translatable.
            query = query.Where(x => x.Name.ToUpper().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

        var paged = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToPagedListAsync(page, cancellationToken);

        return paged.Map(ToResponse);
    }

    private IQueryable<ServiceName> Query() => dbContext.ServiceNames.AsNoTracking();

    private static ServiceNameResponse ToResponse(ServiceName x) =>
        new(x.Id.Value, x.Name, x.Description, x.Status.ToString(), x.CreatedAtUtc, x.ModifiedAtUtc);
}
