using Blueprint.BuildingBlocks.Contracts;
using Blueprint.Modules.Catalog.Application.Abstractions;
using Blueprint.Modules.Catalog.Application.IntegrationEvents;
using Blueprint.Modules.Catalog.Contracts.IntegrationEvents;
using Blueprint.Modules.Catalog.Domain.Products;
using Blueprint.Modules.Ordering.Contracts.IntegrationEvents;

namespace Blueprint.UnitTests.Catalog;

[TestClass]
public sealed class OrderPlacedIntegrationEventHandlerTests
{
    [TestMethod]
    public async Task Handle_WithEnoughStock_ReservesAndPublishesStockReserved()
    {
        var product = Product.Create("A", "A", null, Money.Create(1, "EUR").Value, 5).Value;
        var repository = new FakeProductRepository(product);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new OrderPlacedIntegrationEventHandler(repository, unitOfWork);
        var orderId = Guid.NewGuid();

        await handler.HandleAsync(new OrderPlacedIntegrationEvent(orderId, "alice", [new(product.Id, 2), new(product.Id, 1)]), CancellationToken.None);

        Assert.AreEqual(2, product.StockQuantity);
        var published = Assert.IsInstanceOfType<StockReservedIntegrationEvent>(unitOfWork.Events.Single());
        Assert.AreEqual(orderId, published.OrderId);
    }

    [TestMethod]
    public async Task Handle_WithInsufficientStock_ReservesNothingAndPublishesFailure()
    {
        var a = Product.Create("A", "A", null, Money.Create(1, "EUR").Value, 5).Value;
        var b = Product.Create("B", "B", null, Money.Create(1, "EUR").Value, 0).Value;
        var unitOfWork = new FakeUnitOfWork();
        var handler = new OrderPlacedIntegrationEventHandler(new FakeProductRepository(a, b), unitOfWork);

        await handler.HandleAsync(new OrderPlacedIntegrationEvent(Guid.NewGuid(), "alice", [new(a.Id, 1), new(b.Id, 1)]), CancellationToken.None);

        Assert.AreEqual(5, a.StockQuantity, "Alles-oder-nichts: auch verfügbare Positionen dürfen nicht reserviert werden.");
        Assert.IsInstanceOfType<StockReservationFailedIntegrationEvent>(unitOfWork.Events.Single());
    }

    private sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(products.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Product>>(products.Where(p => ids.Contains(p.Id)).ToList());

        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Product>>(products);

        public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
            Task.FromResult(products.Any(p => p.Sku == sku));

        public void Add(Product product) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : ICatalogUnitOfWork
    {
        public List<IntegrationEvent> Events { get; } = [];

        public void Enqueue(IntegrationEvent integrationEvent) => Events.Add(integrationEvent);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
