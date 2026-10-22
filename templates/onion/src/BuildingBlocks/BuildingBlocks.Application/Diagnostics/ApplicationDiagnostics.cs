namespace BuildingBlocks.Application.Diagnostics;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Tracing and metrics primitives built on System.Diagnostics (no OpenTelemetry dependency).
/// Any OpenTelemetry/Application Insights exporter can subscribe by source/meter name.
/// </summary>
public static class ApplicationDiagnostics
{
    public const string SourceName = "BuildingBlocks.Application";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "application.request.duration",
        unit: "ms",
        description: "Duration of application requests (commands and queries).");

    public static readonly Counter<long> RequestFailures = Meter.CreateCounter<long>(
        "application.request.failures",
        description: "Number of application requests that returned a failure result or threw.");
}
