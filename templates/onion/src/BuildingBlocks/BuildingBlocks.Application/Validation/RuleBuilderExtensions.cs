namespace BuildingBlocks.Application.Validation;

using System.Text.RegularExpressions;

/// <summary>Built-in checks. Add your own as extension methods over <see cref="IRuleBuilder{T,TProperty}"/>.</summary>
public static class RuleBuilderExtensions
{
    public static IRuleBuilder<T, TProperty> NotNull<T, TProperty>(this IRuleBuilder<T, TProperty> rule) =>
        rule.Must((_, v) => v is not null, "'{PropertyName}' must not be null.", "NotNull");

    public static IRuleBuilder<T, TProperty> NotEmpty<T, TProperty>(this IRuleBuilder<T, TProperty> rule) =>
        rule.Must((_, v) => !IsEmpty(v), "'{PropertyName}' must not be empty.", "NotEmpty");

    public static IRuleBuilder<T, TProperty> Empty<T, TProperty>(this IRuleBuilder<T, TProperty> rule) =>
        rule.Must((_, v) => IsEmpty(v), "'{PropertyName}' must be empty.", "Empty");

    public static IRuleBuilder<T, TProperty> Equal<T, TProperty>(this IRuleBuilder<T, TProperty> rule, TProperty expected) =>
        rule.Must((_, v) => EqualityComparer<TProperty>.Default.Equals(v, expected), $"'{{PropertyName}}' must be equal to '{expected}'.", "Equal");

    public static IRuleBuilder<T, string?> MaximumLength<T>(this IRuleBuilder<T, string?> rule, int max) =>
        rule.Must((_, v) => v is null || v.Length <= max, $"'{{PropertyName}}' must be {max} characters or fewer.", "MaximumLength");

    public static IRuleBuilder<T, string?> MinimumLength<T>(this IRuleBuilder<T, string?> rule, int min) =>
        rule.Must((_, v) => v is null || v.Length >= min, $"'{{PropertyName}}' must be at least {min} characters.", "MinimumLength");

    public static IRuleBuilder<T, string?> Length<T>(this IRuleBuilder<T, string?> rule, int min, int max) =>
        rule.Must((_, v) => v is null || (v.Length >= min && v.Length <= max), $"'{{PropertyName}}' must be between {min} and {max} characters.", "Length");

    public static IRuleBuilder<T, string?> Matches<T>(this IRuleBuilder<T, string?> rule, Regex regex) =>
        rule.Must((_, v) => v is null || regex.IsMatch(v), "'{PropertyName}' is not in the correct format.", "Matches");

    public static IRuleBuilder<T, string?> EmailAddress<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must((_, v) => v is null || (System.Net.Mail.MailAddress.TryCreate(v, out var a) && a.Address == v), "'{PropertyName}' is not a valid email address.", "EmailAddress");

    public static IRuleBuilder<T, TProperty> GreaterThan<T, TProperty>(this IRuleBuilder<T, TProperty> rule, TProperty limit)
        where TProperty : IComparable<TProperty> =>
        rule.Must((_, v) => v is null || v.CompareTo(limit) > 0, $"'{{PropertyName}}' must be greater than {limit}.", "GreaterThan");

    public static IRuleBuilder<T, TProperty> GreaterThanOrEqualTo<T, TProperty>(this IRuleBuilder<T, TProperty> rule, TProperty limit)
        where TProperty : IComparable<TProperty> =>
        rule.Must((_, v) => v is null || v.CompareTo(limit) >= 0, $"'{{PropertyName}}' must be greater than or equal to {limit}.", "GreaterThanOrEqualTo");

    public static IRuleBuilder<T, TProperty> LessThan<T, TProperty>(this IRuleBuilder<T, TProperty> rule, TProperty limit)
        where TProperty : IComparable<TProperty> =>
        rule.Must((_, v) => v is null || v.CompareTo(limit) < 0, $"'{{PropertyName}}' must be less than {limit}.", "LessThan");

    public static IRuleBuilder<T, TProperty> LessThanOrEqualTo<T, TProperty>(this IRuleBuilder<T, TProperty> rule, TProperty limit)
        where TProperty : IComparable<TProperty> =>
        rule.Must((_, v) => v is null || v.CompareTo(limit) <= 0, $"'{{PropertyName}}' must be less than or equal to {limit}.", "LessThanOrEqualTo");

    public static IRuleBuilder<T, TProperty> InclusiveBetween<T, TProperty>(this IRuleBuilder<T, TProperty> rule, TProperty from, TProperty to)
        where TProperty : IComparable<TProperty> =>
        rule.Must((_, v) => v is null || (v.CompareTo(from) >= 0 && v.CompareTo(to) <= 0), $"'{{PropertyName}}' must be between {from} and {to}.", "InclusiveBetween");

    public static IRuleBuilder<T, TProperty> IsInEnum<T, TProperty>(this IRuleBuilder<T, TProperty> rule)
        where TProperty : struct, Enum =>
        rule.Must((_, v) => Enum.IsDefined(v), "'{PropertyName}' has a range of values which does not include the given value.", "IsInEnum");

    private static bool IsEmpty<TProperty>(TProperty value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        System.Collections.ICollection c => c.Count == 0,
        System.Collections.IEnumerable e => !e.GetEnumerator().MoveNext(),
        _ => EqualityComparer<TProperty>.Default.Equals(value, default),
    };
}
