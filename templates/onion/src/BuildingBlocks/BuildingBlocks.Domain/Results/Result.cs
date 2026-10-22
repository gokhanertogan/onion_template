namespace BuildingBlocks.Domain.Results;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

/// <summary>Outcome of an operation that returns no value.</summary>
public class Result : IResultFactory<Result>
{
    [JsonConstructor]
    protected internal Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
        {
            throw new ArgumentException("Invalid combination of success flag and error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static implicit operator Result(Error error) => Failure(error);

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>Returns the first failure, or success when every result succeeded.</summary>
    public static Result Combine(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToArray();
        return failures.Length switch
        {
            0 => Success(),
            1 => failures[0],
            _ => Failure(ValidationError.FromResults(failures)),
        };
    }

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error);

    static Result IResultFactory<Result>.Failure(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that returns a value on success.</summary>
public class Result<TValue> : Result, IResultFactory<Result<TValue>>
{
    private readonly TValue? _value;

    [JsonConstructor]
    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    public static Result<TValue> ValidationFailure(Error error) => Failure<TValue>(error);

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    public Result<TOut> Map<TOut>(Func<TValue, TOut> map) =>
        IsSuccess ? Success(map(Value)) : Failure<TOut>(Error);

    public Result<TOut> Bind<TOut>(Func<TValue, Result<TOut>> bind) =>
        IsSuccess ? bind(Value) : Failure<TOut>(Error);

    static Result<TValue> IResultFactory<Result<TValue>>.Failure(Error error) => Failure<TValue>(error);
}
