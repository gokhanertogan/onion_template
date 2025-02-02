using SharedKernel.Interfaces.Repositories;

namespace ServiceName.Application.ServiceName.Repositories;

public interface IServiceNameWriteRepository : IWriteRepository<Domain.Entities.ServiceName, Guid>
{

}