using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Observability;

namespace Order.API.Controllers;

[ApiController]
[Route("api/orders")]
public class InventoryController(ILogger<InventoryController> logger) : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("order-api");

    [HttpGet("{itemId}")]
    public async Task<ActionResult<InventoryResult>> GetInventory(string itemId, [FromQuery] string? chaos, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("OrderApi.GetOrder", ActivityKind.Server);

        var correlationId = HttpContext.Items[CorrelationHeaders.CorrelationId]?.ToString()
                            ?? Guid.NewGuid().ToString("N");
        var userId = HttpContext.Items[CorrelationHeaders.UserId]?.ToString();

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["service"] = "order-api",
            ["correlationId"] = correlationId,
            ["userId"] = userId
        }))
        {
            try
            {
                if (string.Equals(chaos, "timeout", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(TimeSpan.FromSeconds(12), cancellationToken);
                }

                if (string.Equals(chaos, "null", StringComparison.OrdinalIgnoreCase))
                {
                    string? potentiallyNull = null;
                    _ = potentiallyNull!.Length;
                }

                var deterministicFlag = Math.Abs(itemId.GetHashCode()) % 11;
                if (deterministicFlag == 4)
                {
                    throw new InvalidOperationException("Deterministic order rule failure in Order.API.");
                }

                if (Random.Shared.NextDouble() < 0.12)
                {
                    throw new HttpRequestException("Random downstream provider failure for inventory retrieval.");
                }

                var result = new InventoryResult(
                    itemId,
                    Available: true,
                    Warehouse: "eu-central-1",
                    Remaining: Random.Shared.Next(0, 150));

                logger.LogInformation("{@LogContext}", LogContextModel.Create(
                    "Information",
                    $"Order payload resolved for request {itemId}",
                    "order-api",
                    correlationId,
                    userId));

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@LogContext}", LogContextModel.Create(
                    "Error",
                    $"Order.API resolution failed for {itemId}",
                    "order-api",
                    correlationId,
                    userId,
                    ex));

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    itemId,
                    correlationId,
                    error = ex.Message
                });
            }
        }
    }
}
