using System.ComponentModel.DataAnnotations;

namespace Blueprint.Modules.Ordering.Contracts.Orders;

public sealed class PlaceOrderRequest
{
    [Required, MinLength(1)]
    public List<PlaceOrderLineRequest> Lines { get; set; } = [];
}

public sealed class PlaceOrderLineRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}

public sealed record OrderResponse(
    Guid Id,
    string Status,
    string? RejectionReason,
    decimal Total,
    string Currency,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed record OrderLineResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
