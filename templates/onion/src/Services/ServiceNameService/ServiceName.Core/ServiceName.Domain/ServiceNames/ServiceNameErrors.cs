namespace ServiceName.Domain.ServiceNames;

using BuildingBlocks.Domain.Results;

/// <summary>Every business error of the aggregate, with stable machine-readable codes.</summary>
public static class ServiceNameErrors
{
    public static Error NotFound(ServiceNameId id) =>
        Error.NotFound("ServiceName.NotFound", $"ServiceName '{id}' was not found.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("ServiceName.NameAlreadyExists", $"A ServiceName named '{name}' already exists.");

    public static readonly Error NameRequired =
        Error.Validation("ServiceName.NameRequired", "Name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("ServiceName.NameTooLong", $"Name must be {ServiceName.NameMaxLength} characters or fewer.");

    public static readonly Error DescriptionTooLong =
        Error.Validation("ServiceName.DescriptionTooLong", $"Description must be {ServiceName.DescriptionMaxLength} characters or fewer.");

    public static readonly Error AlreadyArchived =
        Error.Conflict("ServiceName.AlreadyArchived", "The ServiceName is already archived.");
}
