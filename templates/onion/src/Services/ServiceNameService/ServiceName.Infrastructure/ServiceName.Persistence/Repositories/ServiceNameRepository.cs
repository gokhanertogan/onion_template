namespace ServiceName.Persistence.Repositories;

using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceName.Domain.ServiceNames;

internal sealed class ServiceNameRepository(ServiceNameDbContext dbContext)
    : Repository<ServiceName, ServiceNameId>(dbContext), IServiceNameRepository
{
    public Task<bool> NameExistsAsync(string name, ServiceNameId? excludeId = null, CancellationToken cancellationToken = default) =>
        dbContext.ServiceNames.AnyAsync(
            x => x.Name == name && (excludeId == null || x.Id != excludeId.Value),
            cancellationToken);
}
