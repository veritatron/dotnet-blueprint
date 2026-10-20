using Blueprint.Modules.Catalog.Application.Abstractions;
using Blueprint.Modules.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Blueprint.Modules.Catalog.Infrastructure.Persistence;

internal sealed class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await dbContext.Products.Where(product => ids.Contains(product.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Products.AsNoTracking().OrderBy(product => product.Name).ToListAsync(cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(product => product.Sku == sku, cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);
}
