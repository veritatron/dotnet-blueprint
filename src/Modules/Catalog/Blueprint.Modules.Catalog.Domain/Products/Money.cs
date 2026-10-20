using Blueprint.BuildingBlocks.Domain;

namespace Blueprint.Modules.Catalog.Domain.Products;

/// <summary>Value Object für Geldbeträge.</summary>
/// <remarks>
/// Value Objects: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects<br/>
/// Persistiert als EF Core Complex Type: https://learn.microsoft.com/ef/core/what-is-new/ef-core-8.0/whatsnew#value-objects-using-complex-types
/// </remarks>
public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return ProductErrors.NegativePrice;
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            return ProductErrors.InvalidCurrency;
        }

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }
}
