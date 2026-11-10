using Grand.Business.Catalog.Events.Handlers;
using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class ProductUnitDetailsTests
{
    private UnitTestFixture _fixture;
    private ProductUnitService _service;

    [TestInitialize]
    public void Init()
    {
        _fixture = new UnitTestFixture();
        _service = StockTestFixture.UnitService(_fixture.Batches, _fixture.Serials, _fixture.Products,
            journal: _fixture.Journal, shipments: _fixture.Shipments);
    }

    private static Product Multiple() => new() {
        ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true
    };

    private static StockChange Batch(int quantity, InventoryMovementType type, string number, string warehouseId) =>
        UnitTestFixture.Change(quantity, type, warehouseId) with { BatchNumber = number };

    private static StockChange Serials(int quantity, InventoryMovementType type, string warehouseId,
        params string[] numbers) =>
        UnitTestFixture.Change(quantity, type, warehouseId) with { SerialNumbers = numbers };

    [TestMethod]
    public async Task UpdateBatchDetails_WritesDetails_KeepsQuantity()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.Batch);
        await _fixture.Apply(product, Batch(5, InventoryMovementType.Receipt, "L1", "w1"));
        var batch = _fixture.BatchesOf(product).Single();
        //a stale copy: the quantity the back office read is not what it writes
        batch.Quantity = 99;
        batch.LocationId = "loc1";
        batch.Supplier = "Acme";
        batch.CustomerComment = "Keep cool";

        await _service.UpdateBatchDetails(batch);

        var stored = _fixture.BatchesOf(product).Single();
        Assert.AreEqual(5, stored.Quantity);
        Assert.AreEqual("loc1", stored.LocationId);
        Assert.AreEqual("Acme", stored.Supplier);
        Assert.AreEqual("Keep cool", stored.CustomerComment);
    }

    [TestMethod]
    public async Task UpdateSerialNumberDetails_WritesDetails_KeepsStatus()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.SerialNumber);
        await _fixture.Apply(product, Serials(1, InventoryMovementType.Receipt, "w1", "A1"));
        var serialNumber = _fixture.SerialsOf(product).Single();
        serialNumber.StatusId = SerialNumberStatus.Scrapped;
        serialNumber.Imei = "490154203237518";
        serialNumber.FirmwareVersion = "1.2";

        await _service.UpdateSerialNumberDetails(serialNumber);

        var stored = _fixture.SerialsOf(product).Single();
        Assert.AreEqual(SerialNumberStatus.InStock, stored.StatusId);
        Assert.AreEqual("490154203237518", stored.Imei);
        Assert.AreEqual("1.2", stored.FirmwareVersion);
    }

    [TestMethod]
    public async Task LedgerMovesSerialNumber_DetailsKept()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.SerialNumber);
        await _fixture.Apply(product, Serials(1, InventoryMovementType.Receipt, "w1", "A1"));
        var serialNumber = _fixture.SerialsOf(product).Single();
        serialNumber.Supplier = "Acme";
        serialNumber.CustomerComment = "Registered for you";
        serialNumber.LocationId = "loc-w1";
        await _service.UpdateSerialNumberDetails(serialNumber);

        var errors = await _fixture.Apply(product, Serials(-1, InventoryMovementType.TransferOut, "w1", "A1"),
            Serials(1, InventoryMovementType.TransferIn, "w2", "A1"));

        Assert.AreEqual(0, errors.Count);
        var stored = _fixture.SerialsOf(product).Single();
        Assert.AreEqual("w2", stored.WarehouseId);
        //the shelf it was on in the warehouse it left is not its place, coming back there included
        Assert.IsNull(stored.LocationId);
        Assert.AreEqual("Acme", stored.Supplier);
        Assert.AreEqual("Registered for you", stored.CustomerComment);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task ScrappedSerialNumberBackInTheSameWarehouse_LocationCleared()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.SerialNumber);
        await _fixture.Apply(product, Serials(1, InventoryMovementType.Receipt, "w1", "A1"));
        var serialNumber = _fixture.SerialsOf(product).Single();
        serialNumber.LocationId = "loc-w1";
        await _service.UpdateSerialNumberDetails(serialNumber);

        await _fixture.Apply(product, Serials(-1, InventoryMovementType.Adjustment, "w1", "A1"));
        Assert.AreEqual("loc-w1", _fixture.SerialsOf(product).Single().LocationId);
        await _fixture.Apply(product, Serials(1, InventoryMovementType.Receipt, "w1", "A1"));

        var stored = _fixture.SerialsOf(product).Single();
        Assert.AreEqual(SerialNumberStatus.InStock, stored.StatusId);
        Assert.IsNull(stored.LocationId);
    }

    [TestMethod]
    public async Task Transfer_NewTargetBatch_TakesDetailsButNotLocation()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.Batch);
        await _fixture.Apply(product, Batch(10, InventoryMovementType.Receipt, "L1", "w1"));
        var source = _fixture.BatchesOf(product).Single();
        source.LocationId = "loc1";
        source.Supplier = "Acme";
        source.SupplierBatchNumber = "S-77";
        source.ManufacturedOnUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        source.CustomerComment = "Keep cool";
        await _service.UpdateBatchDetails(source);

        var errors = await _fixture.Apply(product, Batch(-4, InventoryMovementType.TransferOut, "L1", "w1"),
            Batch(4, InventoryMovementType.TransferIn, "L1", "w2"));

        Assert.AreEqual(0, errors.Count);
        var target = _fixture.BatchesOf(product).Single(x => x.WarehouseId == "w2");
        Assert.AreEqual("Acme", target.Supplier);
        Assert.AreEqual("S-77", target.SupplierBatchNumber);
        Assert.AreEqual(source.ManufacturedOnUtc, target.ManufacturedOnUtc);
        Assert.AreEqual("Keep cool", target.CustomerComment);
        Assert.IsNull(target.LocationId);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task ShippedUnitViews_BatchOfTheItemsWarehouse_SerialComments()
    {
        await _fixture.Products.InsertAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true
        });
        //the same number in another warehouse is another batch, with its own comment
        await _fixture.Batches.InsertAsync(new ProductBatch {
            ProductId = "p1", WarehouseId = "w1", CombinationId = "", Number = "L1", NumberKey = "L1",
            CustomerComment = "from w1"
        });
        await _fixture.Batches.InsertAsync(new ProductBatch {
            ProductId = "p1", WarehouseId = "w2", CombinationId = "", Number = "L1", NumberKey = "L1",
            CustomerComment = "from w2"
        });
        var shipment = new Shipment { Id = "s1" };
        shipment.ShipmentItems.Add(new ShipmentItem {
            Id = "i1", ProductId = "p1", WarehouseId = "w2", Batches = { new ShipmentItemBatch { Number = "l1", Quantity = 3 } }
        });
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "i2", ProductId = "p2", SerialNumbers = { "A1", "A2" } });
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "i3", ProductId = "p3" });
        await _fixture.Serials.InsertAsync(new ProductSerialNumber {
            ProductId = "p2", Number = "A1", NumberKey = "A1", ShipmentId = "s1", CustomerComment = "Registered"
        });
        //returned and shipped again: it names the later shipment, its comment is still its own
        await _fixture.Serials.InsertAsync(new ProductSerialNumber {
            ProductId = "p2", Number = "A2", NumberKey = "A2", ShipmentId = "s9", CustomerComment = "Refurbished"
        });
        //the same number of another product is another unit
        await _fixture.Serials.InsertAsync(new ProductSerialNumber {
            ProductId = "p9", Number = "A1", NumberKey = "A1", CustomerComment = "not this"
        });

        var views = await _service.ShippedUnitViews(shipment);

        Assert.AreEqual(2, views.Count);
        var batch = views["i1"].Single();
        Assert.AreEqual("l1", batch.Number);
        Assert.AreEqual(3, batch.Quantity);
        Assert.AreEqual("from w2", batch.CustomerComment);
        Assert.IsFalse(batch.IsSerialNumber);
        CollectionAssert.AreEqual(new[] { "Registered", "Refurbished" },
            views["i2"].Select(x => x.CustomerComment).ToArray());
        Assert.IsTrue(views["i2"].All(x => x.IsSerialNumber));
    }

    [TestMethod]
    public async Task LocationDeleted_UnitsKeptThereLeftWithoutOne()
    {
        await _fixture.Batches.InsertAsync(new ProductBatch { ProductId = "p1", LocationId = "loc1" });
        await _fixture.Batches.InsertAsync(new ProductBatch { ProductId = "p1", LocationId = "loc2" });
        await _fixture.Serials.InsertAsync(new ProductSerialNumber { ProductId = "p2", LocationId = "loc1" });
        var handler = new WarehouseLocationDeletedEventHandler(_fixture.Batches, _fixture.Serials);

        await handler.Handle(new EntityDeleted<WarehouseLocation>(new WarehouseLocation { Id = "loc1" }),
            CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { null, "loc2" },
            _fixture.Batches.Table.Select(x => x.LocationId).ToArray());
        Assert.IsNull(_fixture.Serials.Table.Single().LocationId);
    }
}
