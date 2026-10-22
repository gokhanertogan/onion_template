namespace BuildingBlocks.Infrastructure.Persistence;

using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;

public static class QueryableExtensions
{
    /// <summary>Executes a count and a page query. Apply ordering before calling to get stable pages.</summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        var total = await query.CountAsync(cancellationToken);
        var items = total == 0
            ? []
            : await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return new PagedList<T>(items, page.Page, page.PageSize, total);
    }
}
