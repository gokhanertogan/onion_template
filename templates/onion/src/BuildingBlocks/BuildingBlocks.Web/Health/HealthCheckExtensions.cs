namespace BuildingBlocks.Web.Health;

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class HealthCheckTags
{
    /// <summary>Process is alive (no dependency checks). Used by liveness probes.</summary>
    public const string Live = "live";

    /// <summary>Dependencies (database, cache, broker...) are reachable. Used by readiness probes.</summary>
    public const string Ready = "ready";
}

internal static class HealthCheckExtensions
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", Options(r => r.Tags.Contains(HealthCheckTags.Live)))
            .AllowAnonymous()
            .DisableRateLimiting()
            .ExcludeFromDescription();

        app.MapHealthChecks("/health/ready", Options(r => r.Tags.Contains(HealthCheckTags.Ready)))
            .AllowAnonymous()
            .DisableRateLimiting()
            .ExcludeFromDescription();
    }

    private static HealthCheckOptions Options(Func<HealthCheckRegistration, bool> predicate) => new()
    {
        Predicate = predicate,
        ResponseWriter = WriteResponseAsync,
    };

    /// <summary>Compact JSON report without exception details (they may contain secrets).</summary>
    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";

        return JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
            }),
        }, cancellationToken: context.RequestAborted);
    }
}
