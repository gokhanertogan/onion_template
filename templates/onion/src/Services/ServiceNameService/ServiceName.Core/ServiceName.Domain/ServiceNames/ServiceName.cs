namespace ServiceName.Domain.ServiceNames;

using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

/// <summary>
/// Aggregate root. State changes only through intention-revealing methods that enforce invariants
/// and raise domain events. No public setters, no anemic model.
/// </summary>
public sealed class ServiceName : AggregateRoot<ServiceNameId>, IAuditable, ISoftDeletable
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    private ServiceName(ServiceNameId id, string name, string? description)
        : base(id)
    {
        Name = name;
        Description = description;
        Status = ServiceNameStatus.Active;
    }

    private ServiceName()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ServiceNameStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Result<ServiceName> Create(string name, string? description)
    {
        var validation = Validate(name, description);
        if (validation.IsFailure)
        {
            return Result.Failure<ServiceName>(validation.Error);
        }

        var serviceName = new ServiceName(ServiceNameId.New(), name.Trim(), Normalize(description));
        serviceName.Raise(new ServiceNameCreatedDomainEvent(serviceName.Id, serviceName.Name));
        return serviceName;
    }

    public Result Update(string name, string? description)
    {
        var validation = Validate(name, description);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Description = Normalize(description);
        Raise(new ServiceNameUpdatedDomainEvent(Id, Name));
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status == ServiceNameStatus.Archived)
        {
            return ServiceNameErrors.AlreadyArchived;
        }

        Status = ServiceNameStatus.Archived;
        Raise(new ServiceNameArchivedDomainEvent(Id));
        return Result.Success();
    }

    /// <summary>Marks the aggregate for deletion; persistence turns it into a soft delete.</summary>
    public void MarkDeleted() => Raise(new ServiceNameDeletedDomainEvent(Id));

    private static Result Validate(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceNameErrors.NameRequired;
        }

        if (name.Trim().Length > NameMaxLength)
        {
            return ServiceNameErrors.NameTooLong;
        }

        if (description?.Length > DescriptionMaxLength)
        {
            return ServiceNameErrors.DescriptionTooLong;
        }

        return Result.Success();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
