using SharedKernel.Interfaces.Repositories;

namespace ServiceName.Application.ServiceName.Repositories;

public interface IServiceNameReadRepository : IReadRepository<Domain.Entities.ServiceName, Guid>
{

}
