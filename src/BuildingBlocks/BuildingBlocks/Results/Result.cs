namespace BuildingBlocks.Results;

public class Result<T>
{
    public T Data { get; set; } = default!;
    public int StatusCode { get; set; }
    public bool IsSuccessful { get; set; }
    public List<string> Errors { get; set; } = [];

    public static Result<T> Success(T data, int statusCode)
    {
        return new Result<T>
        {
            Data = data,
            StatusCode = statusCode,
            IsSuccessful = true
        };
    }

    public static Result<T> Fail(List<string> errors, int statusCode)
    {
        return new Result<T>
        {
            Errors = errors,
            StatusCode = statusCode,
            IsSuccessful = false
        };
    }

    public static Result<T> Fail(string error, int statusCode)
    {
        return new Result<T>
        {
            Errors = [error],
            StatusCode = statusCode,
            IsSuccessful = false
        };
    }
}
