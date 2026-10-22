namespace BuildingBlocks.Application.Validation;

public interface IValidator<in T>
{
    ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
