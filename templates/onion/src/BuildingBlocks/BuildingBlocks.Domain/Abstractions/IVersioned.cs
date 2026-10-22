namespace BuildingBlocks.Domain.Abstractions;

/// <summary>Provider-agnostic optimistic concurrency token.</summary>
public interface IVersioned
{
    Guid Version { get; }
}
