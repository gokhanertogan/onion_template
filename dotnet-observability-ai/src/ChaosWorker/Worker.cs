using System.Diagnostics;
using Shared.Observability;

namespace ChaosWorker;

public class Worker(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<Worker> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("chaos-worker");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue("Chaos:IntervalSeconds", 8);
        var iteration = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            iteration++;
            var correlationId = $"chaos-{DateTime.UtcNow:yyyyMMddHHmmss}-{iteration}";

            using var activity = ActivitySource.StartActivity("ChaosWorker.Iteration", ActivityKind.Internal);
            using (logger.BeginScope(new Dictionary<string, object?>
            {
                ["service"] = "chaos-worker",
                ["correlationId"] = correlationId,
                ["iteration"] = iteration
            }))
            {
                try
                {
                    await RunDeterministicScenarioAsync(iteration, correlationId, stoppingToken);
                    await RunRandomScenarioAsync(correlationId, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{@LogContext}", LogContextModel.Create(
                        "Error",
                        $"Chaos iteration {iteration} encountered an unhandled exception",
                        "chaos-worker",
                        correlationId,
                        exception: ex));
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task RunDeterministicScenarioAsync(int iteration, string correlationId, CancellationToken cancellationToken)
    {
        if (iteration % 7 == 0)
        {
            try
            {
                string? value = null;
                _ = value!.Length;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@LogContext}", LogContextModel.Create(
                    "Error",
                    "Deterministic NullReferenceException injected",
                    "chaos-worker",
                    correlationId,
                    exception: ex));
            }

            return;
        }

        if (iteration % 5 == 0)
        {
            var orderApi = httpClientFactory.CreateClient("orderApi");
            using var timeoutRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/timeout-{iteration}?chaos=timeout");
            timeoutRequest.Headers.Add(CorrelationHeaders.CorrelationId, correlationId);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

            try
            {
                await orderApi.SendAsync(timeoutRequest, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@LogContext}", LogContextModel.Create(
                    "Error",
                    "Deterministic timeout injected via Order.API",
                    "chaos-worker",
                    correlationId,
                    exception: ex));
            }

            return;
        }

        if (iteration % 3 == 0)
        {
            var orderApi = httpClientFactory.CreateClient("orderApi");
            using var failingRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/does-not-exist/{iteration}");
            failingRequest.Headers.Add(CorrelationHeaders.CorrelationId, correlationId);

            var response = await orderApi.SendAsync(failingRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("{@LogContext}", LogContextModel.Create(
                    "Error",
                    $"Deterministic external service failure produced with status {(int)response.StatusCode}",
                    "chaos-worker",
                    correlationId));
            }
        }
    }

    private async Task RunRandomScenarioAsync(string correlationId, CancellationToken cancellationToken)
    {
        var api = httpClientFactory.CreateClient("api");
        var requestId = $"REQ-{DateTime.UtcNow:HHmmss}-{Random.Shared.Next(1000, 9999)}";
        var userId = $"user-{Random.Shared.Next(1, 5)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{requestId}?userId={userId}");
        request.Headers.Add(CorrelationHeaders.CorrelationId, correlationId);
        request.Headers.Add(CorrelationHeaders.UserId, userId);

        var response = await api.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("{@LogContext}", LogContextModel.Create(
                "Error",
                $"Random scenario generated API failure with status {(int)response.StatusCode}",
                "chaos-worker",
                correlationId,
                userId));
            return;
        }

        logger.LogInformation("{@LogContext}", LogContextModel.Create(
            "Information",
            $"Random scenario completed successfully for {requestId}",
            "chaos-worker",
            correlationId,
            userId));
    }
}
