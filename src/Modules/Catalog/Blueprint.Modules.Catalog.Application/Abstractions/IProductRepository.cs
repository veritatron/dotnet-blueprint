using Blueprint.BuildingBlocks.Application.Persistence;
using Blueprint.Modules.Catalog.Domain.Products;

namespace Blueprint.Modules.Catalog.Application.Abstractions;

/// <summary>Repository für das Aggregat <see cref="Product"/> (ein Repository pro Aggregate Root).</summary>
/// <remarks>
/// https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#define-one-repository-per-aggregate
/// </remarks>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken);

    void Add(Product product);
}

/// <summary>Unit of Work + Outbox des Catalog-Moduls.</summary>
public interface ICatalogUnitOfWork : IUnitOfWork, IIntegrationEventOutbox;
