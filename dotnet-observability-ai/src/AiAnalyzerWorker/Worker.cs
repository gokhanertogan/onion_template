using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shared.Ai;
using Shared.Observability;

namespace AiAnalyzerWorker;

public class Worker(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<Worker> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("ai-analyzer-worker");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue("Analyzer:IntervalSeconds", 20);
        var maxTraces = configuration.GetValue("Analyzer:MaxProblematicTraces", 3);
        var maxLogsPerTrace = configuration.GetValue("Analyzer:MaxLogsPerTrace", 30);

        while (!stoppingToken.IsCancellationRequested)
        {
            var correlationId = $"ai-{DateTime.UtcNow:yyyyMMddHHmmss}";
            using var activity = ActivitySource.StartActivity("AiAnalyzer.Tick", ActivityKind.Internal);
            using (logger.BeginScope(new Dictionary<string, object?>
            {
                ["service"] = "ai-analyzer-worker",
                ["correlationId"] = correlationId
            }))
            {
                try
                {
                    var errorLogs = await FetchRecentErrorLogsAsync(correlationId, stoppingToken);
                    var grouped = errorLogs
                        .Where(x => !string.IsNullOrWhiteSpace(x.TraceId))
                        .GroupBy(x => x.TraceId!)
                        .OrderByDescending(group => group.Count())
                        .Take(maxTraces)
                        .Select(group => new
                        {
                            TraceId = group.Key,
                            Logs = group.Take(maxLogsPerTrace).ToList()
                        })
                        .ToList();

                    foreach (var trace in grouped)
                    {
                        var prompt = BuildPrompt(trace.TraceId, trace.Logs);
                        var analysis = await AskOllamaForAnalysisAsync(prompt, correlationId, stoppingToken);
                        var issue = BuildIssueDraft(trace.TraceId, analysis, trace.Logs);
                        await PersistArtifactsAsync(trace.TraceId, analysis, issue, stoppingToken);

                        logger.LogInformation("{@LogContext}", LogContextModel.Create(
                            "Information",
                            $"AI analysis completed for trace {trace.TraceId} with severity {analysis.Severity}",
                            "ai-analyzer-worker",
                            correlationId));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{@LogContext}", LogContextModel.Create(
                        "Error",
                        "AI analyzer iteration failed",
                        "ai-analyzer-worker",
                        correlationId,
                        exception: ex));
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task<List<ErrorLogEntry>> FetchRecentErrorLogsAsync(string correlationId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("elasticsearch");
        var requestBody = new
        {
            size = configuration.GetValue("Analyzer:MaxLogsFetched", 400),
            sort = new object[] { new Dictionary<string, object> { ["@timestamp"] = new { order = "desc" } } },
            query = new
            {
                @bool = new
                {
                    should = new object[]
                    {
                        new { term = new Dictionary<string, string> { ["severity_text.keyword"] = "ERROR" } },
                        new { term = new Dictionary<string, string> { ["severity_text"] = "ERROR" } },
                        new { match = new Dictionary<string, string> { ["body"] = "exception" } },
                        new { match = new Dictionary<string, string> { ["body"] = "failed" } }
                    },
                    minimum_should_match = 1
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/observability-logs*/_search")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(CorrelationHeaders.CorrelationId, correlationId);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var hits = json?["hits"]?["hits"]?.AsArray();
        if (hits is null)
        {
            return [];
        }

        var result = new List<ErrorLogEntry>();
        foreach (var hit in hits)
        {
            var source = hit?["_source"];
            if (source is null)
            {
                continue;
            }

            var traceId = ReadFirstString(source,
                "traceid",
                "traceId",
                "trace_id",
                "trace.id",
                "TraceId");

            var message = ReadFirstString(source,
                "body",
                "message",
                "Body",
                "Message") ?? "unknown error";

            var timestamp = ReadFirstString(source, "@timestamp", "timestamp") ?? DateTimeOffset.UtcNow.ToString("O");
            var service = ReadFirstString(source, "service.name", "service", "Service") ?? "unknown-service";
            var level = ReadFirstString(source, "severity_text", "level", "Level") ?? "ERROR";

            result.Add(new ErrorLogEntry(traceId, timestamp, service, level, message));
        }

        return result;
    }

    private async Task<AiAnalysisResult> AskOllamaForAnalysisAsync(string prompt, string correlationId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("ollama");
        var model = configuration["Ollama:Model"] ?? "mistral:7b-instruct";

        var body = new
        {
            model,
            prompt,
            stream = false,
            format = "json"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(CorrelationHeaders.CorrelationId, correlationId);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var wrapper = JsonNode.Parse(raw);
        var modelResponse = wrapper?["response"]?.GetValue<string>() ?? "{}";

        try
        {
            var analysis = JsonSerializer.Deserialize<AiResponseDto>(modelResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (analysis is null)
            {
                throw new InvalidOperationException("Ollama response could not be deserialized.");
            }

            return new AiAnalysisResult(
                analysis.Title ?? "Runtime failure detected",
                analysis.RootCause ?? "Unknown root cause",
                analysis.Impact ?? "Undetermined impact",
                analysis.Fix ?? "Investigate failing spans and error logs",
                analysis.Severity ?? "Medium",
                analysis.PrDiff ?? "diff --git a/src/Order.API/Controllers/InventoryController.cs b/src/Order.API/Controllers/InventoryController.cs\n--- a/src/Order.API/Controllers/InventoryController.cs\n+++ b/src/Order.API/Controllers/InventoryController.cs\n@@\n-throw new InvalidOperationException(\"Deterministic order rule failure in Order.API.\");\n+logger.LogWarning(\"Order rule rejected request {ItemId}\", itemId);\n+return BadRequest(\"Order rule prevented processing\");");
        }
        catch
        {
            return new AiAnalysisResult(
                "Fallback analysis",
                "LLM returned invalid JSON",
                "Automated diagnostics may be incomplete",
                "Tighten prompt and validate output parser",
                "Medium",
                "diff --git a/src/AiAnalyzerWorker/Worker.cs b/src/AiAnalyzerWorker/Worker.cs\n--- a/src/AiAnalyzerWorker/Worker.cs\n+++ b/src/AiAnalyzerWorker/Worker.cs\n@@\n-var modelResponse = wrapper?[\"response\"]?.GetValue<string>() ?? \"{}\";\n+var modelResponse = wrapper?[\"response\"]?.GetValue<string>()?.Trim() ?? \"{}\";");
        }
    }

    private static string BuildPrompt(string traceId, IReadOnlyCollection<ErrorLogEntry> logs)
    {
        var lines = logs
            .Select(log => $"[{log.Timestamp}] [{log.Service}] [{log.Level}] {log.Message}")
            .ToArray();

        var logBlock = string.Join("\n", lines);

        return $"You are a senior .NET SRE.\n" +
               "Analyze the following trace and logs.\n" +
               "Focus on failure chain, root cause, bad practices, and system impact.\n" +
               "Return ONLY valid JSON in this exact shape:\n" +
               "{\"title\":\"...\",\"root_cause\":\"...\",\"impact\":\"...\",\"fix\":\"...\",\"severity\":\"Low | Medium | High\",\"pr_diff\":\"diff formatted patch\"}\n\n" +
               $"TraceId: {traceId}\n" +
               "Error Logs:\n" +
               logBlock;
    }

    private static GitHubIssueDraft BuildIssueDraft(string traceId, AiAnalysisResult analysis, IReadOnlyCollection<ErrorLogEntry> logs)
    {
        var relatedLogs = string.Join("\n", logs.Take(5).Select(log => $"- [{log.Timestamp}] {log.Service}: {log.Message}"));

        var description = $"## Root cause\n{analysis.RootCause}\n\n" +
                          $"## Impact\n{analysis.Impact}\n\n" +
                          "## Steps to reproduce\n" +
                          "1. Start all services and chaos worker.\n" +
                          "2. Wait for the failing trace to be generated.\n" +
                          $"3. Inspect traceId {traceId} in Kibana.\n\n" +
                          $"## Suggested fix\n{analysis.Fix}\n\n" +
                          "## Related logs\n" +
                          relatedLogs;

        return new GitHubIssueDraft(analysis.Title, description);
    }

    private async Task PersistArtifactsAsync(
        string traceId,
        AiAnalysisResult analysis,
        GitHubIssueDraft issue,
        CancellationToken cancellationToken)
    {
        var outputDir = configuration["Analyzer:OutputDirectory"]
                        ?? Path.Combine(AppContext.BaseDirectory, "analysis-output");

        Directory.CreateDirectory(outputDir);

        var safeTrace = traceId.Replace(':', '_').Replace('/', '_');
        var analysisPath = Path.Combine(outputDir, $"analysis-{safeTrace}.json");
        var issuePath = Path.Combine(outputDir, $"issue-{safeTrace}.md");

        var analysisJson = JsonSerializer.Serialize(new
        {
            title = analysis.Title,
            root_cause = analysis.RootCause,
            impact = analysis.Impact,
            fix = analysis.Fix,
            severity = analysis.Severity,
            pr_diff = analysis.PrDiff
        }, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(analysisPath, analysisJson, cancellationToken);

        var issueMarkdown = $"# {issue.Title}\n\n{issue.Description}\n";
        await File.WriteAllTextAsync(issuePath, issueMarkdown, cancellationToken);
    }

    private static string? ReadFirstString(JsonNode node, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (node[candidate] is JsonValue value)
            {
                return value.GetValue<string>();
            }

            if (candidate.Contains('.'))
            {
                var parts = candidate.Split('.');
                JsonNode? current = node;
                foreach (var part in parts)
                {
                    current = current?[part];
                    if (current is null)
                    {
                        break;
                    }
                }

                if (current is JsonValue nestedValue)
                {
                    return nestedValue.GetValue<string>();
                }
            }
        }

        return null;
    }

    private sealed record ErrorLogEntry(string? TraceId, string Timestamp, string Service, string Level, string Message);

    private sealed class AiResponseDto
    {
        public string? Title { get; set; }
        public string? RootCause { get; set; }
        public string? Impact { get; set; }
        public string? Fix { get; set; }
        public string? Severity { get; set; }
        public string? PrDiff { get; set; }
    }
}
