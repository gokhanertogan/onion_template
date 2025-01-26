using System.Linq.Expressions;
using SharedKernel.Entities;

namespace SharedKernel.Interfaces.Repositories;

public interface IReadRepository<T, TId> : IRepository<T, TId> where T : IEntity<TId>
{
    IQueryable<T> GetAll(bool tracking = true);
    Task<int> CountAsync(CancellationToken cancellationToken, Expression<Func<T, bool>>? method = null);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken, bool tracking = true);
    Task<IEnumerable<T>> GetAllPaginatedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken, bool tracking = true);
    IQueryable<T> GetWhere(Expression<Func<T, bool>> method, bool tracking = true);
    Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> method, CancellationToken cancellationToken, bool tracking = true);
    Task<IEnumerable<T>> GetWherePaginatedAsync(Expression<Func<T, bool>> method, int pageIndex, int pageSize, CancellationToken cancellationToken, bool tracking = true);
    Task<T> GetSingleAsync(Expression<Func<T, bool>> method, CancellationToken cancellationToken, bool tracking = true);
    Task<T> GetByIdAsync(TId id, CancellationToken cancellationToken, bool tracking = true);
    Task<T> GetByIdAsync(TId id, CancellationToken cancellationToken, bool tracking = true, params Expression<Func<T, object>>[] includes);
}

