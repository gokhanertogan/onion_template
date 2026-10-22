namespace ServiceName.Domain.ServiceNames;

using BuildingBlocks.Domain.Abstractions;

public interface IServiceNameRepository : IRepository<ServiceName, ServiceNameId>
{
    Task<bool> NameExistsAsync(string name, ServiceNameId? excludeId = null, CancellationToken cancellationToken = default);
}
