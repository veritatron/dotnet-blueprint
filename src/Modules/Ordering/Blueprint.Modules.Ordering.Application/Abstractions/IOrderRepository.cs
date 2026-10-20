using Blueprint.BuildingBlocks.Application.Persistence;
using Blueprint.Modules.Ordering.Domain.Orders;

namespace Blueprint.Modules.Ordering.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> ListByCustomerAsync(string customerId, CancellationToken cancellationToken);

    void Add(Order order);
}

/// <summary>Unit of Work + Outbox des Ordering-Moduls.</summary>
public interface IOrderingUnitOfWork : IUnitOfWork, IIntegrationEventOutbox;
