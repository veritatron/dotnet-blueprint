using System.Collections.Concurrent;
using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Blueprint.BuildingBlocks.Infrastructure.DomainEvents;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Leitet Domain Events an alle registrierten <see cref="IDomainEventHandler{TDomainEvent}"/> weiter
/// (im aktuellen DI-Scope, also mit derselben Unit of Work).
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation#how-to-subscribe-to-domain-events
/// </remarks>
internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypes = new();

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var wrapperType = WrapperTypes.GetOrAdd(
            domainEvent.GetType(),
            eventType => typeof(DomainEventHandlerWrapper<>).MakeGenericType(eventType));

        var wrapper = (DomainEventHandlerWrapper)Activator.CreateInstance(wrapperType)!;
        await wrapper.HandleAsync(domainEvent, serviceProvider, cancellationToken);
    }

    private abstract class DomainEventHandlerWrapper
    {
        public abstract Task HandleAsync(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class DomainEventHandlerWrapper<TDomainEvent> : DomainEventHandlerWrapper
        where TDomainEvent : IDomainEvent
    {
        public override async Task HandleAsync(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>())
            {
                await handler.HandleAsync((TDomainEvent)domainEvent, cancellationToken);
            }
        }
    }
}

/// <summary>No-op-Dispatcher für Design-Time-Factories (<c>dotnet ef</c>).</summary>
public sealed class DesignTimeDomainEventDispatcher : IDomainEventDispatcher
{
    public static readonly DesignTimeDomainEventDispatcher Instance = new();

    private DesignTimeDomainEventDispatcher()
    {
    }

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}
