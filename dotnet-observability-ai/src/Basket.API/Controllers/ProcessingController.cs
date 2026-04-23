using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Observability;

namespace Basket.API.Controllers;

[ApiController]
[Route("api/baskets")]
public class ProcessingController(IHttpClientFactory httpClientFactory, ILogger<ProcessingController> logger) : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("basket-api");

    [HttpGet("{requestId}")]
    public async Task<ActionResult<ProcessingResult>> ProcessRequest(string requestId, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("BasketApi.ProcessRequest", ActivityKind.Server);

        var correlationId = HttpContext.Items[CorrelationHeaders.CorrelationId]?.ToString()
                            ?? Guid.NewGuid().ToString("N");
        var userId = HttpContext.Items[CorrelationHeaders.UserId]?.ToString();

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["service"] = "basket-api",
            ["correlationId"] = correlationId,
            ["userId"] = userId
        }))
        {
            try
            {
                var client = httpClientFactory.CreateClient("orderApi");
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{requestId}");
                request.Headers.Add(CorrelationHeaders.CorrelationId, correlationId);

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    request.Headers.Add(CorrelationHeaders.UserId, userId);
                }

                logger.LogInformation("{@LogContext}", LogContextModel.Create(
                    "Information",
                    $"Basket.API requesting order details for {requestId}",
                    "basket-api",
                    correlationId,
                    userId));

                using var response = await client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var inventory = await response.Content.ReadFromJsonAsync<InventoryResult>(cancellationToken: cancellationToken);
                if (inventory is null)
                {
                    throw new InvalidOperationException("Order.API returned empty order payload.");
                }

                var processing = new ProcessingResult(requestId, "Processed", inventory, "basket-api");
                return Ok(processing);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@LogContext}", LogContextModel.Create(
                    "Error",
                    $"Basket.API failed to process request {requestId}",
                    "basket-api",
                    correlationId,
                    userId,
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
