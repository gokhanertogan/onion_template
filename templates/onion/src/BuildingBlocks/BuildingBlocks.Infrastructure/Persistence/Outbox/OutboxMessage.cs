namespace BuildingBlocks.Infrastructure.Persistence.Outbox;

using System.Text.Json;
using BuildingBlocks.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class OutboxMessage
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Guid Id { get; init; }

    /// <summary>Assembly-qualified CLR type name (without version) of the domain event.</summary>
    public required string Type { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; }

    public DateTimeOffset? ProcessedOnUtc { get; set; }

    public int Attempts { get; set; }

    public string? Error { get; set; }

    internal static string GetTypeName(Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";

    /// <summary>
    /// Resolves the event type. Only types implementing <see cref="IDomainEvent"/> are accepted,
    /// which prevents deserializing arbitrary types from a tampered row.
    /// </summary>
    internal IDomainEvent? Deserialize()
    {
        var type = System.Type.GetType(Type, throwOnError: false);
        if (type is null || !typeof(IDomainEvent).IsAssignableFrom(type) || type.IsAbstract)
        {
            return null;
        }

        return (IDomainEvent?)JsonSerializer.Deserialize(Content, type, SerializerOptions);
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).HasMaxLength(512).IsRequired();
        builder.Property(m => m.Content).IsRequired().Metadata.SetMaxLength(null); // unbounded (text / nvarchar(max))
        builder.Property(m => m.Error).HasMaxLength(4000);
        builder.HasIndex(m => new { m.ProcessedOnUtc, m.OccurredOnUtc });
    }
}
