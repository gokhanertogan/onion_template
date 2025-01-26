using SharedKernel.Entities;

namespace SharedKernel.Interfaces.Repositories;
public interface IRepository<T, TId> where T : IEntity<TId>
{

}