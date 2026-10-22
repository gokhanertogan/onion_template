namespace BuildingBlocks.Web.ExceptionHandling;

using BuildingBlocks.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Last line of defense: converts unhandled exceptions to problem details.
/// Exception messages are only exposed in Development.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad request"),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (ClientClosedRequest, "Client closed request"),
            _ => (StatusCodes.Status500InternalServerError, "Server failure"),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            LogHandled(logger, exception.GetType().Name, status, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        var detail = status == StatusCodes.Status409Conflict || environment.IsDevelopment()
            ? exception.Message
            : null;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }

    [LoggerMessage(EventId = 3000, Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "{ExceptionType} mapped to {StatusCode} for {Method} {Path}")]
    private static partial void LogHandled(ILogger logger, string exceptionType, int statusCode, string method, string path);
}
