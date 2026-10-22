namespace BuildingBlocks.Application.Validation;

using BuildingBlocks.Domain.Results;

/// <summary>A single failed rule for a property.</summary>
public sealed record ValidationFailure(string PropertyName, string ErrorMessage, string ErrorCode);

public sealed class ValidationResult
{
    public static readonly ValidationResult Success = new([]);

    public ValidationResult(IReadOnlyList<ValidationFailure> errors) => Errors = errors;

    public IReadOnlyList<ValidationFailure> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    /// <summary>Error codes are "Property.Rule"; the web layer groups them by property.</summary>
    public ValidationError ToValidationError() =>
        new([.. Errors.Select(f => Error.Validation($"{f.PropertyName}.{f.ErrorCode}", f.ErrorMessage))]);
}
