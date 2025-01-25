using ProjectName.Domain.Common;

namespace ProjectName.Application.Common.Interfaces.Persistence;

public interface IRepository<T, TId> where T : IEntity<TId>
{

}