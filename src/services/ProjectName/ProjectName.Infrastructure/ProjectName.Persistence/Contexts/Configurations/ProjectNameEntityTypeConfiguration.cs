using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectName.Persistence.Contexts.Configurations;

public class ProjectNameEntityTypeConfiguration : IEntityTypeConfiguration<Domain.Entities.ProjectName>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.ProjectName> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.LastModifiedBy).HasMaxLength(100);
    }
}