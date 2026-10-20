using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.Modules.Ordering.Domain.Orders;

/// <summary>Aggregate Root "Bestellung" mit eigener Zustandsmaschine.</summary>
/// <remarks>
/// Die Bestellung kopiert Produktname und Preis zum Bestellzeitpunkt (eigene Daten pro Bounded Context
/// statt Fremdschlüssel in ein anderes Modul):
/// https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/distributed-data-management
/// </remarks>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = [];

    private Order(Guid id, string customerId, string currency, DateTimeOffset createdAtUtc)
        : base(id)
    {
        CustomerId = customerId;
        Currency = currency;
        CreatedAtUtc = createdAtUtc;
        Status = OrderStatus.Pending;
    }

    private Order()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(line => line.UnitPrice * line.Quantity);

    public static Result<Order> Place(string customerId, IReadOnlyCollection<OrderLineData> lines, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return OrderErrors.MissingCustomer;
        }

        if (lines.Count == 0)
        {
            return OrderErrors.EmptyOrder;
        }

        if (lines.Select(line => line.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            return OrderErrors.MixedCurrencies;
        }

        if (lines.Any(line => line.Quantity <= 0))
        {
            return OrderErrors.InvalidQuantity;
        }

        var order = new Order(Guid.CreateVersion7(), customerId, lines.First().Currency, now);
        foreach (var line in lines)
        {
            order._lines.Add(new OrderLine(Guid.CreateVersion7(), line.ProductId, line.ProductName, line.UnitPrice, line.Quantity));
        }

        order.Raise(new OrderPlacedDomainEvent(order.Id, order.CustomerId, order._lines.ToList()));
        return order;
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            return Result.Failure(OrderErrors.InvalidStatusTransition(Status, OrderStatus.Confirmed));
        }

        Status = OrderStatus.Confirmed;
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (Status != OrderStatus.Pending)
        {
            return Result.Failure(OrderErrors.InvalidStatusTransition(Status, OrderStatus.Rejected));
        }

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        return Result.Success();
    }
}

public sealed class OrderLine : Entity<Guid>
{
    internal OrderLine(Guid id, Guid productId, string productName, decimal unitPrice, int quantity)
        : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    private OrderLine()
    {
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }
}

/// <summary>Eingabedaten für eine Bestellposition (Produktdaten stammen aus dem Catalog-Modul).</summary>
public sealed record OrderLineData(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity);

public enum OrderStatus
{
    Pending,
    Confirmed,
    Rejected,
}

public sealed record OrderPlacedDomainEvent(Guid OrderId, string CustomerId, IReadOnlyList<OrderLine> Lines) : IDomainEvent;
