namespace ServiceName.Domain.ServiceNames;

/// <summary>Strongly typed identifier: prevents mixing up ids of different aggregates.</summary>
public readonly record struct ServiceNameId(Guid Value)
{
    public static ServiceNameId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
