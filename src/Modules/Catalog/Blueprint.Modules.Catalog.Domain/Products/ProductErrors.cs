using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.Modules.Catalog.Domain.Products;

public static class ProductErrors
{
    public static readonly Error NegativePrice = Error.Validation("Catalog.NegativePrice", "Der Preis darf nicht negativ sein.");

    public static readonly Error InvalidCurrency = Error.Validation("Catalog.InvalidCurrency", "Die Währung muss ein ISO-4217-Code sein.");

    public static readonly Error NegativeStock = Error.Validation("Catalog.NegativeStock", "Der Bestand darf nicht negativ sein.");

    public static readonly Error InvalidQuantity = Error.Validation("Catalog.InvalidQuantity", "Die Menge muss größer als 0 sein.");

    public static readonly Error SkuAlreadyExists = Error.Conflict("Catalog.SkuAlreadyExists", "Ein Produkt mit dieser SKU existiert bereits.");

    public static Error NotFound(Guid productId) =>
        Error.NotFound("Catalog.ProductNotFound", $"Produkt '{productId}' wurde nicht gefunden.");

    public static Error InsufficientStock(Guid productId) =>
        Error.Conflict("Catalog.InsufficientStock", $"Für Produkt '{productId}' ist nicht genügend Bestand vorhanden.");
}
