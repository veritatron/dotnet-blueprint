using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.BuildingBlocks.Domain;
using Blueprint.Modules.Catalog.Application.Abstractions;
using Blueprint.Modules.Catalog.Contracts;
using Blueprint.Modules.Catalog.Contracts.Products;
using Blueprint.Modules.Catalog.Domain.Products;

namespace Blueprint.Modules.Catalog.Application.Products;

public sealed record GetProductsQuery : IQuery<IReadOnlyList<ProductResponse>>;

internal sealed class GetProductsQueryHandler(IProductRepository products)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var items = await products.ListAsync(cancellationToken);
        return items.Select(product => product.ToResponse()).ToList();
    }
}

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductResponse>;

internal sealed class GetProductByIdQueryHandler(IProductRepository products)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(query.ProductId, cancellationToken);
        return product is null ? ProductErrors.NotFound(query.ProductId) : product.ToResponse();
    }
}

public sealed record GetProductSnapshotsQuery(IReadOnlyCollection<Guid> ProductIds) : IQuery<IReadOnlyCollection<ProductSnapshot>>;

internal sealed class GetProductSnapshotsQueryHandler(IProductRepository products)
    : IQueryHandler<GetProductSnapshotsQuery, IReadOnlyCollection<ProductSnapshot>>
{
    public async Task<Result<IReadOnlyCollection<ProductSnapshot>>> HandleAsync(GetProductSnapshotsQuery query, CancellationToken cancellationToken)
    {
        var items = await products.GetByIdsAsync(query.ProductIds, cancellationToken);
        return items.Select(product => product.ToSnapshot()).ToList();
    }
}
