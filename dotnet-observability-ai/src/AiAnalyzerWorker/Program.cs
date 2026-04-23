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

builder.Services.AddHttpClient("openai", client =>
{
    var openAiBaseUrl = builder.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com";
    client.BaseAddress = new Uri(openAiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddHttpClient("github", client =>
{
    var githubBaseUrl = builder.Configuration["GitHub:BaseUrl"] ?? "https://api.github.com";
    client.BaseAddress = new Uri(githubBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-observability-ai-analyzer");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
