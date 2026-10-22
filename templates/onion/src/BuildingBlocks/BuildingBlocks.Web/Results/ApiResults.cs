namespace BuildingBlocks.Web.Results;

using BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;

/// <summary>Maps <see cref="Error"/> to RFC 9457 problem details responses.</summary>
public static class ApiResults
{
    public static IResult Problem(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? throw new InvalidOperationException("A successful result can not be converted to a problem.")
            : Problem(result.Error);
    }

    public static IResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error is ValidationError validation)
        {
            var errors = validation.Errors
                .GroupBy(e => PropertyOf(e.Code), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray(), StringComparer.Ordinal);

            return HttpResults.ValidationProblem(
                errors,
                detail: error.Description,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title: "Validation failed",
                extensions: extensions);
        }

        var (status, title, type) = error.Type switch
        {
            ErrorType.Validation or ErrorType.Problem => (StatusCodes.Status400BadRequest, "Bad request", "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized", "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden", "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found", "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict", "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => (StatusCodes.Status500InternalServerError, "Server failure", "https://tools.ietf.org/html/rfc9110#section-15.6.1"),
        };

        // Never leak details of unexpected failures.
        var detail = status >= 500 ? "An unexpected error occurred." : error.Description;
        return HttpResults.Problem(detail: detail, statusCode: status, title: title, type: type, extensions: extensions);
    }

    /// <summary>Validation codes are "Property.Rule"; group by the property path.</summary>
    private static string PropertyOf(string code)
    {
        var index = code.LastIndexOf('.');
        return index > 0 ? code[..index] : code;
    }
}
