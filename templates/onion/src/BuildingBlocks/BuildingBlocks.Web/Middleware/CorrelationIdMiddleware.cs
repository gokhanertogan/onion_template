namespace BuildingBlocks.Web.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Accepts a safe X-Correlation-ID from the caller (or falls back to the W3C trace id),
/// echoes it on the response and adds it to every log entry of the request.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].ToString();
        if (!IsValid(correlationId))
        {
            correlationId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
        }

        context.TraceIdentifier = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    // Prevent header/log injection: short, alphanumeric plus '-' and '_' only.
    private static bool IsValid(string value) =>
        value.Length is > 0 and <= MaxLength && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
