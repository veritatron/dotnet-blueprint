using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.Modules.Ordering.Application.Orders;
using Blueprint.Modules.Ordering.Contracts.Orders;
using Blueprint.Modules.Ordering.Domain.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Blueprint.Modules.Ordering.Application;

/// <summary>Registriert die Use Cases des Ordering-Moduls.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddOrderingApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<PlaceOrderCommand, OrderResponse>, PlaceOrderCommandHandler>();
        services.AddScoped<IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderResponse>>, GetMyOrdersQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderByIdQuery, OrderResponse>, GetOrderByIdQueryHandler>();
        services.AddScoped<IDomainEventHandler<OrderPlacedDomainEvent>, OrderPlacedDomainEventHandler>();
        return services;
    }
}
