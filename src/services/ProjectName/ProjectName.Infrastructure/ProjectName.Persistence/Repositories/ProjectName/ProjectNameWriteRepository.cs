using ProjectName.Application.ProjectName.Repositories;
using ProjectName.Persistence.Contexts;
using ProjectName.Persistence.Repositories.Common;

namespace ProjectName.Persistence.Repositories.ProjectName;
public class ProjectNameWriteRepository(ApplicationDbContext context)
: WriteRepository<Domain.Entities.ProjectName, Guid>(context), IProjectNameWriteRepository
{

}