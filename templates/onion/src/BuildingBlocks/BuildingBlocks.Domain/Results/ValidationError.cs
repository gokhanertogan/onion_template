namespace BuildingBlocks.Domain.Results;

/// <summary>Aggregates multiple field-level errors into one validation error.</summary>
public sealed record ValidationError : Error
{
    public ValidationError(IReadOnlyList<Error> errors)
        : base("Validation.General", "One or more validation errors occurred.", ErrorType.Validation) =>
        Errors = errors;

    public IReadOnlyList<Error> Errors { get; }

    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new([.. results.Where(r => r.IsFailure).Select(r => r.Error)]);
}
