using ProjectName.Application.Common.Interfaces.Persistence;

namespace ProjectName.Application.ProjectName.Repositories;

public interface IProjectNameReadRepository : IReadRepository<Domain.Entities.ProjectName, Guid>
{

}
