namespace BuildingBlocks.Application.Validation;

using System.Runtime.CompilerServices;

/// <summary>Fluent API for configuring the checks of one property.</summary>
public interface IRuleBuilder<T, out TProperty>
{
    /// <summary>Adds a predicate. <paramref name="message"/> may contain {PropertyName}.</summary>
    IRuleBuilder<T, TProperty> Must(Func<T, TProperty, bool> predicate, string message, string errorCode);

    IRuleBuilder<T, TProperty> MustAsync(Func<T, TProperty, CancellationToken, ValueTask<bool>> predicate, string message, string errorCode);

    /// <summary>Overrides the message of the last added check.</summary>
    IRuleBuilder<T, TProperty> WithMessage(string message);

    /// <summary>Overrides the error code of the last added check.</summary>
    IRuleBuilder<T, TProperty> WithErrorCode(string errorCode);

    /// <summary>Overrides the property name used in messages and error codes.</summary>
    IRuleBuilder<T, TProperty> WithName(string propertyName);

    /// <summary>Runs the rule only when the condition holds.</summary>
    IRuleBuilder<T, TProperty> When(Func<T, bool> condition);

    /// <summary>Stops evaluating further checks of this rule after the first failure.</summary>
    IRuleBuilder<T, TProperty> StopOnFirstFailure();

    /// <summary>Validates a complex (possibly nullable) property with its own validator. Null values are skipped.</summary>
    IRuleBuilder<T, TProperty> SetValidator<TChild>(IValidator<TChild> validator);
}

internal interface IValidationRule<in T>
{
    IAsyncEnumerable<ValidationFailure> ValidateAsync(T instance, string prefix, CancellationToken cancellationToken);
}

internal sealed class Check<T, TProperty>(Func<T, TProperty, CancellationToken, ValueTask<bool>> predicate, string message, string errorCode)
{
    public Func<T, TProperty, CancellationToken, ValueTask<bool>> Predicate { get; } = predicate;

    public string Message { get; set; } = message;

    public string ErrorCode { get; set; } = errorCode;
}

internal abstract class RuleBase<T, TProperty>(string propertyName) : IRuleBuilder<T, TProperty>, IValidationRule<T>
{
    private readonly List<Check<T, TProperty>> _checks = [];
    private readonly List<Func<object, CancellationToken, ValueTask<ValidationResult>>> _childValidators = [];
    private Func<T, bool>? _condition;
    private bool _stopOnFirstFailure;
    private string _propertyName = propertyName;

    public IRuleBuilder<T, TProperty> Must(Func<T, TProperty, bool> predicate, string message, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _checks.Add(new((instance, value, _) => ValueTask.FromResult(predicate(instance, value)), message, errorCode));
        return this;
    }

    public IRuleBuilder<T, TProperty> MustAsync(Func<T, TProperty, CancellationToken, ValueTask<bool>> predicate, string message, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _checks.Add(new(predicate, message, errorCode));
        return this;
    }

    public IRuleBuilder<T, TProperty> WithMessage(string message)
    {
        LastCheck().Message = message;
        return this;
    }

    public IRuleBuilder<T, TProperty> WithErrorCode(string errorCode)
    {
        LastCheck().ErrorCode = errorCode;
        return this;
    }

    public IRuleBuilder<T, TProperty> WithName(string propertyName)
    {
        _propertyName = propertyName;
        return this;
    }

    public IRuleBuilder<T, TProperty> When(Func<T, bool> condition)
    {
        _condition = condition;
        return this;
    }

    public IRuleBuilder<T, TProperty> StopOnFirstFailure()
    {
        _stopOnFirstFailure = true;
        return this;
    }

    public IRuleBuilder<T, TProperty> SetValidator<TChild>(IValidator<TChild> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _childValidators.Add((value, ct) => value is TChild child ? validator.ValidateAsync(child, ct) : ValueTask.FromResult(ValidationResult.Success));
        return this;
    }

    public async IAsyncEnumerable<ValidationFailure> ValidateAsync(
        T instance,
        string prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_condition is not null && !_condition(instance))
        {
            yield break;
        }

        foreach (var (name, value) in GetValues(instance, Combine(prefix, _propertyName)))
        {
            var failed = false;
            foreach (var check in _checks)
            {
                if (await check.Predicate(instance, value, cancellationToken))
                {
                    continue;
                }

                failed = true;
                yield return new ValidationFailure(name, check.Message.Replace("{PropertyName}", name, StringComparison.Ordinal), check.ErrorCode);

                if (_stopOnFirstFailure)
                {
                    break;
                }
            }

            if (failed || value is null)
            {
                continue;
            }

            foreach (var child in _childValidators)
            {
                var result = await child(value, cancellationToken);
                foreach (var failure in result.Errors)
                {
                    yield return failure with { PropertyName = $"{name}.{failure.PropertyName}" };
                }
            }
        }
    }

    protected abstract IEnumerable<(string Name, TProperty Value)> GetValues(T instance, string name);

    private static string Combine(string prefix, string name) => prefix.Length == 0 ? name : $"{prefix}.{name}";

    private Check<T, TProperty> LastCheck() =>
        _checks.Count > 0 ? _checks[^1] : throw new InvalidOperationException("Add a check before configuring its message or code.");
}

internal sealed class PropertyRule<T, TProperty>(string propertyName, Func<T, TProperty> accessor)
    : RuleBase<T, TProperty>(propertyName)
{
    protected override IEnumerable<(string Name, TProperty Value)> GetValues(T instance, string name)
    {
        yield return (name, accessor(instance));
    }
}

internal sealed class CollectionRule<T, TElement>(string propertyName, Func<T, IEnumerable<TElement>?> accessor)
    : RuleBase<T, TElement>(propertyName)
{
    protected override IEnumerable<(string Name, TElement Value)> GetValues(T instance, string name) =>
        (accessor(instance) ?? []).Select((element, index) => ($"{name}[{index}]", element));
}
