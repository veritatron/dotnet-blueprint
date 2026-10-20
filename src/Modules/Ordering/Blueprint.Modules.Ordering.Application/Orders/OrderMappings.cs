using Blueprint.Modules.Ordering.Contracts.Orders;
using Blueprint.Modules.Ordering.Domain.Orders;

namespace Blueprint.Modules.Ordering.Application.Orders;

internal static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order) => new(
        order.Id,
        order.Status.ToString(),
        order.RejectionReason,
        order.Total,
        order.Currency,
        order.CreatedAtUtc,
        order.Lines.Select(line => new OrderLineResponse(line.ProductId, line.ProductName, line.UnitPrice, line.Quantity)).ToList());
}
