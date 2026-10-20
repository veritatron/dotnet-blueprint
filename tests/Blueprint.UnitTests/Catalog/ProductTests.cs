using Blueprint.Modules.Catalog.Domain.Products;

namespace Blueprint.UnitTests.Catalog;

[TestClass]
public sealed class ProductTests
{
    private static Product CreateProduct(int stock = 10) =>
        Product.Create("bp-001", "Mug", null, Money.Create(12.9m, "eur").Value, stock).Value;

    [TestMethod]
    public void Create_NormalizesSkuAndCurrency_AndRaisesDomainEvent()
    {
        var product = CreateProduct();

        Assert.AreEqual("BP-001", product.Sku);
        Assert.AreEqual("EUR", product.Price.Currency);
        Assert.IsInstanceOfType<ProductCreatedDomainEvent>(product.DomainEvents.Single());
    }

    [TestMethod]
    public void Money_WithNegativeAmount_Fails()
    {
        var result = Money.Create(-1, "EUR");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ProductErrors.NegativePrice, result.Error);
    }

    [TestMethod]
    public void ReserveStock_WithSufficientStock_DecreasesStock()
    {
        var product = CreateProduct(stock: 5);

        var result = product.ReserveStock(3);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, product.StockQuantity);
    }

    [TestMethod]
    public void ReserveStock_WithInsufficientStock_FailsAndKeepsStock()
    {
        var product = CreateProduct(stock: 1);

        var result = product.ReserveStock(2);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("Catalog.InsufficientStock", result.Error.Code);
        Assert.AreEqual(1, product.StockQuantity);
    }
}
