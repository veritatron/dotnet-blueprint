using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.Modules.Catalog.Application.Abstractions;
using Blueprint.Modules.Catalog.Contracts.IntegrationEvents;
using Blueprint.Modules.Ordering.Contracts.IntegrationEvents;

namespace Blueprint.Modules.Catalog.Application.IntegrationEvents;

/// <summary>
/// Reserviert den Bestand für eine neue Bestellung und meldet das Ergebnis per Integration Event zurück
/// (Choreografie-basierte Saga).
/// </summary>
/// <remarks>
/// Sagas/Choreografie: https://learn.microsoft.com/azure/architecture/reference-architectures/saga/saga#choreography
/// </remarks>
internal sealed class OrderPlacedIntegrationEventHandler(IProductRepository products, ICatalogUnitOfWork unitOfWork)
    : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    public async Task HandleAsync(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var productIds = integrationEvent.Lines.Select(line => line.ProductId).Distinct().ToList();
        var productsById = (await products.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);

        var quantities = integrationEvent.Lines
            .GroupBy(line => line.ProductId)
            .Select(group => (ProductId: group.Key, Quantity: group.Sum(line => line.Quantity)))
            .ToList();

        // Erst alles prüfen, dann reservieren → alles-oder-nichts innerhalb des Aggregat-Sets.
        var missing = quantities.FirstOrDefault(q => !productsById.TryGetValue(q.ProductId, out var p) || !p.CanReserve(q.Quantity));
        if (missing != default)
        {
            unitOfWork.Enqueue(new StockReservationFailedIntegrationEvent(
                integrationEvent.OrderId,
                $"Nicht genügend Bestand für Produkt {missing.ProductId}."));
            return;
        }

        foreach (var (productId, quantity) in quantities)
        {
            productsById[productId].ReserveStock(quantity);
        }

        unitOfWork.Enqueue(new StockReservedIntegrationEvent(integrationEvent.OrderId));
    }
}
