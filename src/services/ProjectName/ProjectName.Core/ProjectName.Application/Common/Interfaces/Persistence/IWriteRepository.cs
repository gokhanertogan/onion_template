using System.Linq.Expressions;
using ProjectName.Domain.Common;

namespace ProjectName.Application.Common.Interfaces.Persistence;

public interface IWriteRepository<T, TId> : IRepository<T, TId> where T : IEntity<TId>
{
    Task<bool> AddAsync(T model, CancellationToken cancellationToken);
    Task<bool> AddRangeAsync(List<T> datas, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(TId id, CancellationToken cancellationToken);
    bool RemoveRange(List<T> datas);
    bool Update(T model);
    Task SaveAsync(CancellationToken cancellationToken);
}
