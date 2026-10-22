namespace BuildingBlocks.Application.Behaviors;

using Microsoft.Extensions.Logging;

/// <summary>High-performance, source-generated log messages (no boxing, no parsing at runtime).</summary>
internal static partial class BehaviorLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "Processing request {RequestName}")]
    public static partial void Processing(ILogger logger, string requestName);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Completed request {RequestName} in {ElapsedMilliseconds:0.0} ms")]
    public static partial void Completed(ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Request {RequestName} failed with {ErrorCode}: {ErrorDescription} ({ElapsedMilliseconds:0.0} ms)")]
    public static partial void Failed(ILogger logger, string requestName, string errorCode, string errorDescription, double elapsedMilliseconds);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Request {RequestName} threw an unhandled exception")]
    public static partial void Exception(ILogger logger, Exception exception, string requestName);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Slow request {RequestName} took {ElapsedMilliseconds:0.0} ms (threshold {ThresholdMilliseconds} ms)")]
    public static partial void Slow(ILogger logger, string requestName, double elapsedMilliseconds, double thresholdMilliseconds);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Information, Message = "Validation failed for {RequestName} with {ErrorCount} error(s)")]
    public static partial void ValidationFailed(ILogger logger, string requestName, int errorCount);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Information, Message = "Replaying idempotent command {RequestName} for key {IdempotencyKey}")]
    public static partial void IdempotentReplay(ILogger logger, string requestName, Guid idempotencyKey);
}
