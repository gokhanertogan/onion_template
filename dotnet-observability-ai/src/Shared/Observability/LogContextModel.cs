using System.Diagnostics;

namespace Shared.Observability;

public sealed record LogContextModel(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string Service,
    string? TraceId,
    string? SpanId,
    string CorrelationId,
    string? UserId,
    string? Exception)
{
    public static LogContextModel Create(
        string level,
        string message,
        string service,
        string correlationId,
        string? userId = null,
        Exception? exception = null)
    {
        var current = Activity.Current;

        return new LogContextModel(
            DateTimeOffset.UtcNow,
            level,
            message,
            service,
            current?.TraceId.ToString(),
            current?.SpanId.ToString(),
            correlationId,
            userId,
            exception?.ToString());
    }
}
