using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["Service:Name"] ?? "order-api";
var otlpEndpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317";

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddSource(serviceName)
        .AddAspNetCoreInstrumentation(options => options.RecordException = true)
        .AddHttpClientInstrumentation(options => options.RecordException = true)
        .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint)));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("CorrelationMiddleware");
    var correlationId = context.Request.Headers.TryGetValue(CorrelationHeaders.CorrelationId, out var header)
        ? header.ToString()
        : Guid.NewGuid().ToString("N");
    var userId = context.Request.Headers.TryGetValue(CorrelationHeaders.UserId, out var userHeader)
        ? userHeader.ToString()
        : null;

    context.Items[CorrelationHeaders.CorrelationId] = correlationId;
    context.Items[CorrelationHeaders.UserId] = userId;
    context.Response.Headers[CorrelationHeaders.CorrelationId] = correlationId;

    using (logger.BeginScope(new Dictionary<string, object?>
    {
        ["service"] = serviceName,
        ["correlationId"] = correlationId,
        ["userId"] = userId
    }))
    {
        await next();
    }
});

app.UseAuthorization();
app.MapControllers();
app.Run();
