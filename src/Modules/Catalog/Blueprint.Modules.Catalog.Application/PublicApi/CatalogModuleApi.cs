using Blueprint.BuildingBlocks.Application.Messaging;
using Blueprint.Modules.Catalog.Application.Products;
using Blueprint.Modules.Catalog.Contracts;

namespace Blueprint.Modules.Catalog.Application.PublicApi;

/// <summary>In-Process-Implementierung der öffentlichen Modul-API (Modulith-Betrieb).</summary>
internal sealed class CatalogModuleApi(IQueryHandler<GetProductSnapshotsQuery, IReadOnlyCollection<ProductSnapshot>> handler)
    : ICatalogModuleApi
{
    public async Task<IReadOnlyCollection<ProductSnapshot>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new GetProductSnapshotsQuery(productIds), cancellationToken);
        return result.Value;
    }
}
