using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Domain;
using Blueprint.Modules.Ordering.Application.Abstractions;
using Blueprint.Modules.Ordering.Contracts.Orders;
using Blueprint.Modules.Ordering.Domain.Orders;

namespace Blueprint.Modules.Ordering.Application.Orders;

public sealed record GetMyOrdersQuery(string CustomerId) : IQuery<IReadOnlyList<OrderResponse>>;

internal sealed class GetMyOrdersQueryHandler(IOrderRepository orders) : IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> HandleAsync(GetMyOrdersQuery query, CancellationToken cancellationToken)
    {
        var items = await orders.ListByCustomerAsync(query.CustomerId, cancellationToken);
        return items.Select(order => order.ToResponse()).ToList();
    }
}

public sealed record GetOrderByIdQuery(Guid OrderId, string CustomerId) : IQuery<OrderResponse>;

internal sealed class GetOrderByIdQueryHandler(IOrderRepository orders) : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(query.OrderId, cancellationToken);

        // Fremde Bestellungen werden wie nicht existierende behandelt (keine Informationspreisgabe).
        return order is null || order.CustomerId != query.CustomerId
            ? OrderErrors.NotFound(query.OrderId)
            : order.ToResponse();
    }
}
