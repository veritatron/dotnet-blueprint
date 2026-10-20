using Blueprint.Modules.Catalog.Contracts;
using Blueprint.Modules.Catalog.Contracts.Products;
using Blueprint.Modules.Catalog.Domain.Products;

namespace Blueprint.Modules.Catalog.Application.Products;

internal static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) => new(
        product.Id, product.Sku, product.Name, product.Description, product.Price.Amount, product.Price.Currency, product.StockQuantity);

    public static ProductSnapshot ToSnapshot(this Product product) => new(
        product.Id, product.Sku, product.Name, product.Price.Amount, product.Price.Currency, product.StockQuantity);
}
