using Blueprint.BuildingBlocks.Contracts;

namespace Blueprint.BuildingBlocks.Application.Messaging;

/// <summary>
/// Verarbeitet ein Integration Event eines anderen Moduls.
/// Handler rufen <c>SaveChangesAsync</c> NICHT selbst auf: Die Infrastruktur (Inbox) speichert die Änderungen
/// zusammen mit dem Inbox-Eintrag atomar und stellt so idempotente Verarbeitung sicher.
/// </summary>
/// <remarks>
/// Idempotenz von Integration-Event-Handlern:
/// https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/subscribe-events#designing-idempotent-integration-event-handlers
/// </remarks>
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    Task HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
