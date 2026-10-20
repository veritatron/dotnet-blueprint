using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Domain;
using Blueprint.Modules.Catalog.Contracts;
using Blueprint.Modules.Ordering.Application.Abstractions;
using Blueprint.Modules.Ordering.Contracts.Orders;
using Blueprint.Modules.Ordering.Domain.Orders;

namespace Blueprint.Modules.Ordering.Application.Orders;

public sealed record PlaceOrderCommand(string CustomerId, IReadOnlyList<(Guid ProductId, int Quantity)> Lines) : ICommand<OrderResponse>;

/// <summary>
/// Gibt eine Bestellung auf. Holt aktuelle Produktdaten SYNCHRON über die öffentliche Modul-API des Catalog-Moduls
/// (<see cref="ICatalogModuleApi"/>) – im Modulith in-process, nach Auslagerung per HTTP.
/// Die Bestandsreservierung erfolgt anschließend ASYNCHRON über Integration Events.
/// </summary>
internal sealed class PlaceOrderCommandHandler(
    ICatalogModuleApi catalog,
    IOrderRepository orders,
    IOrderingUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<PlaceOrderCommand, OrderResponse>
{
    public async Task<Result<OrderResponse>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var productIds = command.Lines.Select(line => line.ProductId).Distinct().ToList();
        var products = (await catalog.GetProductsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);

        var unknown = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (unknown.Count != 0)
        {
            return OrderErrors.UnknownProducts(unknown);
        }

        var lines = command.Lines
            .Select(line =>
            {
                var product = products[line.ProductId];
                return new OrderLineData(product.Id, product.Name, product.UnitPrice, product.Currency, line.Quantity);
            })
            .ToList();

        var order = Order.Place(command.CustomerId, lines, timeProvider.GetUtcNow());
        if (order.IsFailure)
        {
            return order.Error;
        }

        orders.Add(order.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Value.ToResponse();
    }
}
