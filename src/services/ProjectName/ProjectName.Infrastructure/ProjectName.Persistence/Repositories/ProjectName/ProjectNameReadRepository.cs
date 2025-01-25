using ProjectName.Application.ProjectName.Repositories;
using ProjectName.Persistence.Contexts;
using ProjectName.Persistence.Repositories.Common;

namespace ProjectName.Persistence.Repositories.ProjectName;

public class ProjectNameReadRepository(ApplicationDbContext context)
: ReadRepository<Domain.Entities.ProjectName, Guid>(context), IProjectNameReadRepository
{

}