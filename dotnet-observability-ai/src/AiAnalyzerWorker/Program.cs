using AiAnalyzerWorker;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

var serviceName = builder.Configuration["Service:Name"] ?? "ai-analyzer-worker";
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
        .AddHttpClientInstrumentation(options => options.RecordException = true)
        .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint)));

builder.Services.AddHttpClient("elasticsearch", client =>
{
    var elasticBaseUrl = builder.Configuration["Elasticsearch:BaseUrl"] ?? "http://localhost:9200";
    client.BaseAddress = new Uri(elasticBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient("ollama", client =>
{
    var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
