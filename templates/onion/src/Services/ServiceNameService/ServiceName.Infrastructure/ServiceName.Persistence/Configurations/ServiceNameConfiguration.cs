namespace ServiceName.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceName.Domain.ServiceNames;

internal sealed class ServiceNameConfiguration : IEntityTypeConfiguration<ServiceName>
{
    public void Configure(EntityTypeBuilder<ServiceName> builder)
    {
        builder.ToTable("ServiceNames");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ServiceNameId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name).HasMaxLength(ServiceName.NameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(ServiceName.DescriptionMaxLength);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.CreatedBy).HasMaxLength(128);
        builder.Property(x => x.ModifiedBy).HasMaxLength(128);
        builder.Property(x => x.DeletedBy).HasMaxLength(128);

        builder.HasIndex(x => x.Name);
    }
}
