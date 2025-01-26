using SharedKernel.Entities;

namespace ProjectName.Domain.Entities;

public class ProjectName : Entity<Guid>
{
    public string Name { get; set; } = default!;
}