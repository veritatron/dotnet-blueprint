using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.Modules.Catalog.Domain.Products;

/// <summary>Aggregate Root "Produkt" im Katalog.</summary>
/// <remarks>
/// Rich Domain Model mit gekapseltem Zustand (private Setter, Factory-Methode):
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model#the-domain-entity-pattern
/// </remarks>
public sealed class Product : AggregateRoot<Guid>
{
    private Product(Guid id, string sku, string name, string? description, Money price, int stockQuantity)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
    }

    private Product()
    {
    }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Money Price { get; private set; } = null!;

    public int StockQuantity { get; private set; }

    public static Result<Product> Create(string sku, string name, string? description, Money price, int initialStock)
    {
        if (initialStock < 0)
        {
            return ProductErrors.NegativeStock;
        }

        var product = new Product(Guid.CreateVersion7(), sku.Trim().ToUpperInvariant(), name.Trim(), description?.Trim(), price, initialStock);
        product.Raise(new ProductCreatedDomainEvent(product.Id));
        return product;
    }

    public bool CanReserve(int quantity) => quantity > 0 && StockQuantity >= quantity;

    public Result ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(ProductErrors.InvalidQuantity);
        }

        if (StockQuantity < quantity)
        {
            return Result.Failure(ProductErrors.InsufficientStock(Id));
        }

        StockQuantity -= quantity;
        return Result.Success();
    }
}
