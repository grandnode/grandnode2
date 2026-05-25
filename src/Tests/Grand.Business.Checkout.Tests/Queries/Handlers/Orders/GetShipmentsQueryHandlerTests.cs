using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetShipmentsQueryHandlerTests
{
    private GetShipmentsQueryHandler _handler;
    private MongoDBRepositoryTest<Shipment> _repository;
    private DateTime _baseDate;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Shipment>();

        _baseDate = DateTime.UtcNow;

        // Insert then Update to persist custom CreatedOnUtc (Insert always overwrites with UtcNow)
        var shipment1 = new Shipment { StoreId = "store1", VendorId = "vendor1", OrderId = "order1", TrackingNumber = "TN-001", ShippedDateUtc = _baseDate };
        shipment1.ShipmentItems.Add(new ShipmentItem { WarehouseId = "wh1" });
        _repository.Insert(shipment1);
        shipment1.CreatedOnUtc = _baseDate.AddDays(-3);
        _repository.Update(shipment1);

        var shipment2 = new Shipment { StoreId = "store1", VendorId = "vendor2", OrderId = "order2", TrackingNumber = "TN-002", ShippedDateUtc = null };
        shipment2.ShipmentItems.Add(new ShipmentItem { WarehouseId = "wh2" });
        _repository.Insert(shipment2);
        shipment2.CreatedOnUtc = _baseDate.AddDays(-1);
        _repository.Update(shipment2);

        var shipment3 = new Shipment { StoreId = "store2", VendorId = "vendor1", OrderId = "order3", TrackingNumber = "TN-003", ShippedDateUtc = _baseDate };
        shipment3.ShipmentItems.Add(new ShipmentItem { WarehouseId = "wh1" });
        _repository.Insert(shipment3);
        shipment3.CreatedOnUtc = _baseDate.AddDays(-2);
        _repository.Update(shipment3);

        _handler = new GetShipmentsQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Handle_NoFilters_ReturnsAllShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery(), CancellationToken.None);
        Assert.AreEqual(3, result.Count());
    }

    [TestMethod]
    public async Task Handle_FilterByStoreId_ReturnsMatchingShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { StoreId = "store1" }, CancellationToken.None);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(s => s.StoreId == "store1"));
    }

    [TestMethod]
    public async Task Handle_FilterByVendorId_ReturnsMatchingShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { VendorId = "vendor1" }, CancellationToken.None);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(s => s.VendorId == "vendor1"));
    }

    [TestMethod]
    public async Task Handle_FilterByOrderId_ReturnsSingleShipment()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { OrderId = "order1" }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("order1", result.First().OrderId);
    }

    [TestMethod]
    public async Task Handle_FilterByTrackingNumber_ReturnsSingleShipment()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { TrackingNumber = "TN-002" }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("TN-002", result.First().TrackingNumber);
    }

    [TestMethod]
    public async Task Handle_LoadNotShipped_ReturnsOnlyUnshippedShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { LoadNotShipped = true }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.IsNull(result.First().ShippedDateUtc);
    }

    [TestMethod]
    public async Task Handle_FilterByWarehouseId_ReturnsShipmentsWithMatchingItems()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { WarehouseId = "wh1" }, CancellationToken.None);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(s => s.ShipmentItems.Any(i => i.WarehouseId == "wh1")));
    }

    [TestMethod]
    public async Task Handle_FilterByCreatedFromUtc_ReturnsShipmentsAfterDate()
    {
        var cutoff = _baseDate.AddDays(-1).AddHours(-1);
        var result = await _handler.Handle(new GetShipmentsQuery { CreatedFromUtc = cutoff }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.IsTrue(result.All(s => s.CreatedOnUtc >= cutoff));
    }

    [TestMethod]
    public async Task Handle_FilterByCreatedToUtc_ReturnsShipmentsBeforeDate()
    {
        var cutoff = _baseDate.AddDays(-2).AddHours(1);
        var result = await _handler.Handle(new GetShipmentsQuery { CreatedToUtc = cutoff }, CancellationToken.None);
        Assert.AreEqual(2, result.Count()); // shipment1 (-3days) and shipment3 (-2days) are both <= cutoff (-2days+1hr)
        Assert.IsTrue(result.All(s => s.CreatedOnUtc <= cutoff));
    }

    [TestMethod]
    public async Task Handle_ResultIsOrderedByCreatedOnUtcDescending()
    {
        var result = (await _handler.Handle(new GetShipmentsQuery(), CancellationToken.None)).ToList();
        for (var i = 0; i < result.Count - 1; i++)
            Assert.IsTrue(result[i].CreatedOnUtc >= result[i + 1].CreatedOnUtc);
    }
}
