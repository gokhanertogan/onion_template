namespace ServiceName.UnitTests.Application;

using ServiceName.Domain.ServiceNames;

/// <summary>Hand-written fake: no mocking library needed.</summary>
internal sealed class FakeServiceNameRepository : IServiceNameRepository
{
    public List<ServiceName> Items { get; } = [];

    public Task<ServiceName?> GetByIdAsync(ServiceNameId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

    public Task<bool> NameExistsAsync(string name, ServiceNameId? excludeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(x => x.Name == name && x.Id != excludeId));

    public void Add(ServiceName aggregate) => Items.Add(aggregate);

    public void Remove(ServiceName aggregate) => Items.Remove(aggregate);
}
