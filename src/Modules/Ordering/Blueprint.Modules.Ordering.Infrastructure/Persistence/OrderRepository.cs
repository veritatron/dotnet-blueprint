using Blueprint.Modules.Ordering.Application.Abstractions;
using Blueprint.Modules.Ordering.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Blueprint.Modules.Ordering.Infrastructure.Persistence;

internal sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(string customerId, CancellationToken cancellationToken) =>
        await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(Order order) => dbContext.Orders.Add(order);
}
