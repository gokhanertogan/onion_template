using ServiceName.Application.ServiceName.Repositories;
using ServiceName.Persistence.Contexts;
using ServiceName.Persistence.Repositories.Common;

namespace ServiceName.Persistence.Repositories.ServiceName;

public class ServiceNameReadRepository(ApplicationDbContext context)
: ReadRepository<Domain.Entities.ServiceName, Guid>(context), IServiceNameReadRepository
{

}