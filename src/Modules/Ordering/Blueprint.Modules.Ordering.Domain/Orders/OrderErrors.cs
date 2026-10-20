using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.Modules.Ordering.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error MissingCustomer = Error.Validation("Ordering.MissingCustomer", "Kein Kunde angegeben.");

    public static readonly Error EmptyOrder = Error.Validation("Ordering.EmptyOrder", "Eine Bestellung benötigt mindestens eine Position.");

    public static readonly Error MixedCurrencies = Error.Validation("Ordering.MixedCurrencies", "Alle Positionen müssen dieselbe Währung haben.");

    public static readonly Error InvalidQuantity = Error.Validation("Ordering.InvalidQuantity", "Die Menge muss größer als 0 sein.");

    public static Error NotFound(Guid orderId) =>
        Error.NotFound("Ordering.OrderNotFound", $"Bestellung '{orderId}' wurde nicht gefunden.");

    public static Error UnknownProducts(IEnumerable<Guid> productIds) =>
        Error.Validation("Ordering.UnknownProducts", $"Unbekannte Produkte: {string.Join(", ", productIds)}.");

    public static Error InvalidStatusTransition(OrderStatus from, OrderStatus to) =>
        Error.Conflict("Ordering.InvalidStatusTransition", $"Statuswechsel von {from} nach {to} ist nicht erlaubt.");
}
