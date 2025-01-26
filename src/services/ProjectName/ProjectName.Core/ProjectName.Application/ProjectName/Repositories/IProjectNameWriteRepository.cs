using SharedKernel.Interfaces.Repositories;

namespace ProjectName.Application.ProjectName.Repositories;

public interface IProjectNameWriteRepository : IWriteRepository<Domain.Entities.ProjectName, Guid>
{

}