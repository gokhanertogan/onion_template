namespace BuildingBlocks.Infrastructure.Persistence.Outbox;

using BuildingBlocks.Application.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Publishes pending outbox messages to in-process domain event handlers (at-least-once).
/// For multiple instances, use a provider-specific row lock (e.g. FOR UPDATE SKIP LOCKED) or a single worker.
/// </summary>
public sealed partial class OutboxProcessor<TDbContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor<TDbContext>> logger)
    : BackgroundService
    where TDbContext : AppDbContextBase
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(settings.PollingInterval, timeProvider);
        do
        {
            try
            {
                while (await ProcessBatchAsync(settings, stoppingToken) == settings.BatchSize)
                {
                    // Drain backlog without waiting for the next tick.
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // The worker must survive transient failures.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogBatchFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task<int> ProcessBatchAsync(OutboxOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.Attempts < settings.MaxAttempts)
            .OrderBy(m => m.Id) // Version 7 GUIDs are time ordered.
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Attempts++;
            try
            {
                var domainEvent = message.Deserialize()
                    ?? throw new InvalidOperationException($"Unknown or disallowed outbox message type '{message.Type}'.");

                await dispatcher.DispatchAsync(domainEvent, cancellationToken);
                message.ProcessedOnUtc = timeProvider.GetUtcNow();
                message.Error = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
#pragma warning disable CA1031 // A poison message must not block the rest of the batch.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                var error = ex.ToString();
                message.Error = error.Length > 4000 ? error[..4000] : error;
                LogMessageFailed(logger, ex, message.Id, message.Type, message.Attempts);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Error, Message = "Outbox batch processing failed")]
    private static partial void LogBatchFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Outbox message {MessageId} ({MessageType}) failed on attempt {Attempt}")]
    private static partial void LogMessageFailed(ILogger logger, Exception exception, Guid messageId, string messageType, int attempt);
}
