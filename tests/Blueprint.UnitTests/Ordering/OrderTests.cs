using Blueprint.Modules.Ordering.Domain.Orders;

namespace Blueprint.UnitTests.Ordering;

[TestClass]
public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Order PlaceOrder() => Order.Place(
        "alice",
        [new OrderLineData(Guid.NewGuid(), "Mug", 10m, "EUR", 2), new OrderLineData(Guid.NewGuid(), "Pen", 1.5m, "EUR", 4)],
        Now).Value;

    [TestMethod]
    public void Place_CalculatesTotal_IsPending_AndRaisesOrderPlaced()
    {
        var order = PlaceOrder();

        Assert.AreEqual(OrderStatus.Pending, order.Status);
        Assert.AreEqual(26m, order.Total);
        var domainEvent = Assert.IsInstanceOfType<OrderPlacedDomainEvent>(order.DomainEvents.Single());
        Assert.HasCount(2, domainEvent.Lines);
    }

    [TestMethod]
    public void Place_WithMixedCurrencies_Fails()
    {
        var result = Order.Place(
            "alice",
            [new OrderLineData(Guid.NewGuid(), "A", 1, "EUR", 1), new OrderLineData(Guid.NewGuid(), "B", 1, "USD", 1)],
            Now);

        Assert.AreEqual(OrderErrors.MixedCurrencies, result.Error);
    }

    [TestMethod]
    public void Confirm_AfterReject_IsNotAllowed()
    {
        var order = PlaceOrder();
        order.Reject("kein Bestand");

        var result = order.Confirm();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(OrderStatus.Rejected, order.Status);
        Assert.AreEqual("kein Bestand", order.RejectionReason);
    }
}
