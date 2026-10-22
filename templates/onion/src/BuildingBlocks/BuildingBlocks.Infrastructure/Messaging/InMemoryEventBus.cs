namespace BuildingBlocks.Infrastructure.Messaging;

using System.Collections.Concurrent;
using System.Threading.Channels;
using BuildingBlocks.Application.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-process event bus backed by a bounded channel. Suitable for a modular monolith and local development.
/// Replace with a broker-backed <see cref="IEventBus"/> for cross-process delivery.
/// </summary>
internal sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<object> _channel = Channel.CreateBounded<object>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    public ChannelReader<object> Reader => _channel.Reader;

    public ValueTask PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return _channel.Writer.WriteAsync(integrationEvent, cancellationToken);
    }
}

internal sealed partial class InMemoryEventBusWorker(
    InMemoryEventBus bus,
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryEventBusWorker> logger)
    : BackgroundService
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> Invokers = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in bus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var invoker = Invokers.GetOrAdd(message.GetType(), CreateInvoker);
                await invoker(scope.ServiceProvider, message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // One failing consumer must not stop the bus.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogHandlerFailed(logger, ex, message.GetType().Name);
            }
        }
    }

    private static Func<IServiceProvider, object, CancellationToken, Task> CreateInvoker(Type eventType) =>
        typeof(InMemoryEventBusWorker)
            .GetMethod(nameof(InvokeHandlersAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(eventType)
            .CreateDelegate<Func<IServiceProvider, object, CancellationToken, Task>>();

    private static async Task InvokeHandlersAsync<TEvent>(IServiceProvider serviceProvider, object message, CancellationToken cancellationToken)
        where TEvent : class
    {
        foreach (var handler in serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
        {
            await handler.Handle((TEvent)message, cancellationToken);
        }
    }

    [LoggerMessage(EventId = 2200, Level = LogLevel.Error, Message = "Integration event handler failed for {EventType}")]
    private static partial void LogHandlerFailed(ILogger logger, Exception exception, string eventType);
}
