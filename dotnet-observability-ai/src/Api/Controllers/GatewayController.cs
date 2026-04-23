using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Observability;

namespace Api.Controllers;

[ApiController]
[Route("api/orders")]
public class GatewayController(IHttpClientFactory httpClientFactory, ILogger<GatewayController> logger) : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("api-gateway");

    [HttpGet("{requestId}")]
    public async Task<ActionResult<GatewayResponse>> GetOrder(string requestId, [FromQuery] string? userId, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("Gateway.GetOrder", ActivityKind.Server);
        var correlationId = HttpContext.Items[CorrelationHeaders.CorrelationId]?.ToString()
                            ?? Guid.NewGuid().ToString("N");

        var resolvedUserId = userId ?? HttpContext.Items[CorrelationHeaders.UserId]?.ToString();

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["service"] = "api-gateway",
            ["correlationId"] = correlationId,
            ["userId"] = resolvedUserId
        }))
        {
            try
            {
                var client = httpClientFactory.CreateClient("basketApi");
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/baskets/{requestId}");
                request.Headers.Add(CorrelationHeaders.CorrelationId, correlationId);

                if (!string.IsNullOrWhiteSpace(resolvedUserId))
                {
                    request.Headers.Add(CorrelationHeaders.UserId, resolvedUserId);
                }

                logger.LogInformation("{@LogContext}", LogContextModel.Create(
                    "Information",
                    $"Forwarding request {requestId} from API Gateway to Basket.API",
                    "api-gateway",
                    correlationId,
                    resolvedUserId));

                using var response = await client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadFromJsonAsync<ProcessingResult>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    cancellationToken);

                if (body is null)
                {
                    throw new InvalidOperationException("Basket.API returned an empty response.");
                }

                var gatewayResponse = new GatewayResponse(
                    requestId,
                    "Accepted",
                    "api-gateway",
                    body);

                return Ok(gatewayResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@LogContext}", LogContextModel.Create(
                    "Error",
                    $"Gateway request {requestId} failed",
                    "api-gateway",
                    correlationId,
                    resolvedUserId,
                    ex));

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    requestId,
                    correlationId,
                    error = ex.Message
                });
            }
        }
    }
}
