using Blueprint.BuildingBlocks.Contracts;

namespace Blueprint.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Abstraktion des Event Bus. Implementierungen: In-Memory (Start), später z. B. RabbitMQ, Kafka oder Azure Service Bus.
/// Module kennen nur diese Abstraktion (bzw. die Outbox) – der Transport ist reine Konfiguration.
/// </summary>
/// <remarks>
/// Event-Bus-Abstraktion: https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/integration-event-based-microservice-communications#the-event-bus<br/>
/// Referenzimplementierung (eShop, RabbitMQ): https://github.com/dotnet/eShop/tree/main/src/EventBusRabbitMQ
/// </remarks>
public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
