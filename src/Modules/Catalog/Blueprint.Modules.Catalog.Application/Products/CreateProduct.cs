using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Domain;
using Blueprint.Modules.Catalog.Application.Abstractions;
using Blueprint.Modules.Catalog.Contracts.Products;
using Blueprint.Modules.Catalog.Domain.Products;

namespace Blueprint.Modules.Catalog.Application.Products;

public sealed record CreateProductCommand(string Sku, string Name, string? Description, decimal Price, string Currency, int InitialStock)
    : ICommand<ProductResponse>;

internal sealed class CreateProductCommandHandler(IProductRepository products, ICatalogUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var price = Money.Create(command.Price, command.Currency);
        if (price.IsFailure)
        {
            return price.Error;
        }

        var product = Product.Create(command.Sku, command.Name, command.Description, price.Value, command.InitialStock);
        if (product.IsFailure)
        {
            return product.Error;
        }

        if (await products.SkuExistsAsync(product.Value.Sku, cancellationToken))
        {
            return ProductErrors.SkuAlreadyExists;
        }

        products.Add(product.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Value.ToResponse();
    }
}
