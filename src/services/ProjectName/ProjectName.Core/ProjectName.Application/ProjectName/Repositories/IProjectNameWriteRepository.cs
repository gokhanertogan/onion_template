using ProjectName.Application.Common.Interfaces.Persistence;

namespace ProjectName.Application.ProjectName.Repositories;

public interface IProjectNameWriteRepository : IWriteRepository<Domain.Entities.ProjectName, Guid>
{

}