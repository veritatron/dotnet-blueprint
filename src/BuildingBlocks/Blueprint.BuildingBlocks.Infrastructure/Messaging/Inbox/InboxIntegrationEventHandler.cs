using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Contracts;
using Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging.Inbox;

/// <summary>Nicht-generischer Einstiegspunkt, über den der Event-Bus-Consumer einen Handler aufruft.</summary>
public interface IIntegrationEventDispatchTarget
{
    Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Decorator (Inbox-Pattern): verhindert doppelte Verarbeitung bei at-least-once-Zustellung und speichert
/// die Änderungen des Handlers zusammen mit dem Inbox-Eintrag in einer Transaktion.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/subscribe-events#idempotency-in-update-message-events
/// </remarks>
internal sealed class InboxIntegrationEventHandler<TEvent, THandler, TContext>(
    THandler innerHandler,
    TContext dbContext,
    TimeProvider timeProvider) : IIntegrationEventDispatchTarget
    where TEvent : IntegrationEvent
    where THandler : IIntegrationEventHandler<TEvent>
    where TContext : ModuleDbContext
{
    private static readonly string HandlerName = typeof(THandler).FullName!;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await dbContext.InboxMessages.AnyAsync(
            message => message.EventId == integrationEvent.Id && message.HandlerName == HandlerName,
            cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        await innerHandler.HandleAsync((TEvent)integrationEvent, cancellationToken);

        dbContext.InboxMessages.Add(InboxMessage.Create(integrationEvent.Id, HandlerName, timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
