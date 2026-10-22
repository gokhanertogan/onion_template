namespace BuildingBlocks.Application.Behaviors;

using System.Diagnostics;
using BuildingBlocks.Application.Diagnostics;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;

/// <summary>
/// Outermost behavior: structured logging, distributed tracing span and metrics per request.
/// Request payloads are deliberately not logged (PII / secrets).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger, TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const double SlowRequestThresholdMs = 500;
    private static readonly string RequestName = typeof(TRequest).Name;
    private static readonly string RequestKind = typeof(IBaseCommand).IsAssignableFrom(typeof(TRequest)) ? "command" : "query";

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(RequestName);
        activity?.SetTag("app.request.name", RequestName);
        activity?.SetTag("app.request.kind", RequestKind);

        using var scope = logger.BeginScope(new Dictionary<string, object> { ["RequestName"] = RequestName });
        BehaviorLog.Processing(logger, RequestName);

        var start = timeProvider.GetTimestamp();
        var outcome = "success";
        try
        {
            var response = await next(cancellationToken);
            var elapsed = timeProvider.GetElapsedTime(start).TotalMilliseconds;

            if (response is Result { IsFailure: true } failure)
            {
                outcome = "failure";
                activity?.SetStatus(ActivityStatusCode.Error, failure.Error.Code);
                activity?.SetTag("app.error.code", failure.Error.Code);
                ApplicationDiagnostics.RequestFailures.Add(1, new("request", RequestName), new("error.type", failure.Error.Type.ToString()));
                BehaviorLog.Failed(logger, RequestName, failure.Error.Code, failure.Error.Description, elapsed);
            }
            else
            {
                BehaviorLog.Completed(logger, RequestName, elapsed);
            }

            if (elapsed > SlowRequestThresholdMs)
            {
                BehaviorLog.Slow(logger, RequestName, elapsed, SlowRequestThresholdMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            outcome = "exception";
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            ApplicationDiagnostics.RequestFailures.Add(1, new("request", RequestName), new("error.type", ex.GetType().Name));
            BehaviorLog.Exception(logger, ex, RequestName);
            throw;
        }
        finally
        {
            ApplicationDiagnostics.RequestDuration.Record(
                timeProvider.GetElapsedTime(start).TotalMilliseconds,
                new("request", RequestName),
                new("kind", RequestKind),
                new("outcome", outcome));
        }
    }
}
