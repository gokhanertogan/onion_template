namespace BuildingBlocks.Application.Validation;

using System.Linq.Expressions;

/// <summary>
/// Minimal fluent validator. Declare rules in the constructor:
/// <code>RuleFor(x => x.Name).NotEmpty().MaximumLength(100);</code>
/// </summary>
public abstract class AbstractValidator<T> : IValidator<T>
{
    private readonly List<IValidationRule<T>> _rules = [];

    public async ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        List<ValidationFailure>? failures = null;
        foreach (var rule in _rules)
        {
            await foreach (var failure in rule.ValidateAsync(instance, string.Empty, cancellationToken))
            {
                (failures ??= []).Add(failure);
            }
        }

        return failures is null ? ValidationResult.Success : new ValidationResult(failures);
    }

    protected IRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var rule = new PropertyRule<T, TProperty>(GetPropertyName(expression), expression.Compile());
        _rules.Add(rule);
        return rule;
    }

    /// <summary>Applies the rules to every element of a collection property.</summary>
    protected IRuleBuilder<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>?>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var rule = new CollectionRule<T, TElement>(GetPropertyName(expression), expression.Compile());
        _rules.Add(rule);
        return rule;
    }

    private static string GetPropertyName(LambdaExpression expression)
    {
        var parts = new Stack<string>();
        var body = expression.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary ? unary.Operand : expression.Body;

        while (body is MemberExpression member)
        {
            parts.Push(member.Member.Name);
            body = member.Expression!;
        }

        return parts.Count > 0
            ? string.Join('.', parts)
            : throw new ArgumentException("Expression must be a property access, e.g. x => x.Name.", nameof(expression));
    }
}
