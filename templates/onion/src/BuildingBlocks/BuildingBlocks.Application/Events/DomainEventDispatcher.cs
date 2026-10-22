namespace BuildingBlocks.Application.Events;

using System.Collections.Concurrent;
using BuildingBlocks.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var invoker = Invokers.GetOrAdd(
            domainEvent.GetType(),
            static type => (HandlerInvoker)Activator.CreateInstance(typeof(HandlerInvoker<>).MakeGenericType(type))!);

        return invoker.InvokeAsync(domainEvent, serviceProvider, cancellationToken);
    }

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TEvent> : HandlerInvoker
        where TEvent : IDomainEvent
    {
        public override async Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
            {
                await handler.Handle((TEvent)domainEvent, cancellationToken);
            }
        }
    }
}
