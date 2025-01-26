using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProjectName.Persistence.Contexts;
using SharedKernel.Entities;
using SharedKernel.Interfaces.Repositories;

namespace ProjectName.Persistence.Repositories.Common;

public class ReadRepository<T, TId>(ApplicationDbContext context) : IReadRepository<T, TId> where T : class, IEntity<TId>
{
    private readonly ApplicationDbContext _context = context;
    public DbSet<T> Table => _context.Set<T>();
    public Task<int> CountAsync(CancellationToken cancellationToken, Expression<Func<T, bool>>? method = null)
    {
        var query = Table.AsQueryable();
        if (method != null)
            query = query.Where(method);

        return query.CountAsync(cancellationToken);
    }

    public IQueryable<T> GetAll(bool tracking = true)
    {
        var query = Table.AsQueryable();
        if (!tracking)
            query = query.AsNoTracking();

        return query;
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken, bool tracking = true)
    {
        var query = Table.AsQueryable();
        if (!tracking)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllPaginatedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken, bool tracking = true)
    {
        var query = GetAll(tracking);
        query = query.Skip(pageSize * pageIndex).Take(pageSize);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<T> GetByIdAsync(TId id, CancellationToken cancellationToken, bool tracking = true)
    {
        var query = Table.AsQueryable();
        if (!tracking)
            query = query.AsNoTracking();

        return (await query.FirstOrDefaultAsync(data => data.Id!.Equals(id)))!;
    }

    public async Task<T> GetByIdAsync(TId id, CancellationToken cancellationToken, bool tracking = true, params Expression<Func<T, object>>[] includes)
    {
        var query = Table.AsQueryable().AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return (await query.FirstOrDefaultAsync(data => data.Id!.Equals(id)))!;
    }

    public async Task<T> GetSingleAsync(Expression<Func<T, bool>> method, CancellationToken cancellationToken, bool tracking = true)
    {
        var query = Table.AsQueryable();
        if (!tracking)
            query = query.AsNoTracking();

        return (await query.FirstOrDefaultAsync(method))!;
    }

    public IQueryable<T> GetWhere(Expression<Func<T, bool>> method, bool tracking = true)
    {
        var query = Table.Where(method);
        if (!tracking)
            query = query.AsNoTracking();
        return query;
    }

    public async Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> method, CancellationToken cancellationToken, bool tracking = true)
    {
        return await GetWhere(method, tracking).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetWherePaginatedAsync(Expression<Func<T, bool>> method, int pageIndex, int pageSize, CancellationToken cancellationToken, bool tracking = true)
    {
        return await GetWhere(method, tracking).Skip(pageSize * pageIndex).Take(pageSize).ToListAsync(cancellationToken);
    }
}
