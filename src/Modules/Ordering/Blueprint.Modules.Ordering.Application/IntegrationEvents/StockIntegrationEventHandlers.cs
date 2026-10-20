using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.Modules.Catalog.Contracts.IntegrationEvents;
using Blueprint.Modules.Ordering.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Blueprint.Modules.Ordering.Application.IntegrationEvents;

internal sealed partial class StockReservedIntegrationEventHandler(IOrderRepository orders, ILogger<StockReservedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StockReservedIntegrationEvent>
{
    public async Task HandleAsync(StockReservedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(integrationEvent.OrderId, cancellationToken);
        if (order is null)
        {
            LogOrderNotFound(integrationEvent.OrderId);
            return;
        }

        order.Confirm();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bestellung {OrderId} für StockReserved nicht gefunden")]
    private partial void LogOrderNotFound(Guid orderId);
}

internal sealed partial class StockReservationFailedIntegrationEventHandler(IOrderRepository orders, ILogger<StockReservationFailedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StockReservationFailedIntegrationEvent>
{
    public async Task HandleAsync(StockReservationFailedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(integrationEvent.OrderId, cancellationToken);
        if (order is null)
        {
            LogOrderNotFound(integrationEvent.OrderId);
            return;
        }

        order.Reject(integrationEvent.Reason);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bestellung {OrderId} für StockReservationFailed nicht gefunden")]
    private partial void LogOrderNotFound(Guid orderId);
}
