using ServiceName.Application.ServiceName.Repositories;
using ServiceName.Persistence.Contexts;
using ServiceName.Persistence.Repositories.Common;

namespace ServiceName.Persistence.Repositories.ServiceName;
public class ServiceNameWriteRepository(ApplicationDbContext context)
: WriteRepository<Domain.Entities.ServiceName, Guid>(context), IServiceNameWriteRepository
{

}