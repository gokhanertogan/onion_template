using SharedKernel.Entities;

namespace ServiceName.Domain.Entities;

public class ServiceName : Entity<Guid>
{
    public string Name { get; set; } = default!;
}