using ProjectName.Domain.Common;

namespace ProjectName.Domain.Entities;

public class ProjectName : Entity<Guid>
{
    public string Name { get; set; } = default!;
}