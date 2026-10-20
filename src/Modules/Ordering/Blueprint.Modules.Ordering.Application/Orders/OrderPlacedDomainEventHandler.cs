using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.Modules.Ordering.Application.Abstractions;
using Blueprint.Modules.Ordering.Contracts.IntegrationEvents;
using Blueprint.Modules.Ordering.Domain.Orders;

namespace Blueprint.Modules.Ordering.Application.Orders;

/// <summary>
/// Übersetzt das modul-interne Domain Event in ein öffentliches Integration Event und legt es in die Outbox
/// (gleiche Transaktion wie die Bestellung).
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation#domain-events-versus-integration-events
/// </remarks>
internal sealed class OrderPlacedDomainEventHandler(IOrderingUnitOfWork unitOfWork)
    : IDomainEventHandler<OrderPlacedDomainEvent>
{
    public Task HandleAsync(OrderPlacedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        unitOfWork.Enqueue(new OrderPlacedIntegrationEvent(
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.Lines.Select(line => new OrderPlacedLine(line.ProductId, line.Quantity)).ToList()));

        return Task.CompletedTask;
    }
}
