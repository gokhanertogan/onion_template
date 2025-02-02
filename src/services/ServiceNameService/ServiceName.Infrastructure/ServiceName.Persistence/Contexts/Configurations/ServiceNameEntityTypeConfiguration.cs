using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceName.Persistence.Contexts.Configurations;

public class ServiceNameEntityTypeConfiguration : IEntityTypeConfiguration<Domain.Entities.ServiceName>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.ServiceName> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.LastModifiedBy).HasMaxLength(100);
    }
}