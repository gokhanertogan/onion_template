namespace BuildingBlocks.Application.Behaviors;

using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;

/// <summary>
/// Runs every validator registered for the request. Invalid requests short-circuit with a
/// <see cref="ValidationError"/> result: no exceptions are used for control flow.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultFactory<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        List<ValidationFailure>? failures = null;
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                (failures ??= []).AddRange(result.Errors);
            }
        }

        if (failures is null)
        {
            return await next(cancellationToken);
        }

        BehaviorLog.ValidationFailed(logger, typeof(TRequest).Name, failures.Count);
        return TResponse.Failure(new ValidationResult(failures).ToValidationError());
    }
}
