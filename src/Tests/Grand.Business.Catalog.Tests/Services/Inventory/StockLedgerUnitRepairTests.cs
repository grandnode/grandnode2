using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class StockLedgerUnitRepairTests
{
    private static readonly DateTime January = new(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc);
    private UnitTestFixture _fixture;

    [TestInitialize]
    public void Init()
    {
        _fixture = new UnitTestFixture();
    }

    private static Product Single() => new()
        { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1" };

    private static Product Multiple() => new()
        { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true };

    private static StockChange Counted(string warehouseId, params string[] numbers) =>
        UnitTestFixture.Change(0, InventoryMovementType.StockCount, warehouseId) with {
            CountedSerialNumbers = numbers
        };

    private ProductSerialNumber Serial(Product product, string number) =>
        _fixture.SerialsOf(product).Single(x => x.NumberKey == UnitNumbers.Key(number));

    /// <summary>
    ///     An entry whose unit and product writes were lost: it is in the journal, nothing else has it
    /// </summary>
    private Task LostEntry(Product product, int revision, int quantity, InventoryMovementType type,
        string batchNumber = null, string[] serialNumbers = null, string objectType = "InventoryDocument",
        string objectId = "d1", string positionId = "l1") =>
        _fixture.Journal.InsertAsync(new InventoryJournal {
            ProductId = product.Id, CombinationId = "", WarehouseId = "w1", StockRevision = revision,
            InQty = Math.Max(0, quantity), OutQty = Math.Max(0, -quantity), MovementTypeId = type,
            BatchNumber = batchNumber, SerialNumbers = serialNumbers ?? [], ExpiryDateUtc = batchNumber != null ? January : null,
            CreateDateUtc = DateTime.UtcNow, ObjectType = objectType, ObjectId = objectId, PositionId = positionId
        });

    [TestMethod]
    public async Task Repair_UnitWriteLost_ReplaysOnce()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch);
        await LostEntry(product, 1, 5, InventoryMovementType.Receipt, "L1");

        var errors = await _fixture.Apply(product);

        Assert.AreEqual(0, errors.Count);
        var batch = _fixture.BatchesOf(product).Single();
        Assert.AreEqual(5, batch.Quantity);
        Assert.AreEqual(January, batch.ExpiryDateUtc);
        Assert.AreEqual(1, batch.StockRevision);
        Assert.AreEqual(5, _fixture.Stored(product).StockQuantity);
        Assert.AreEqual(1, _fixture.Stored(product).StockRevision);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Repair_ProductWriteLost_UnitsNotReplayedTwice()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch);
        await LostEntry(product, 1, 5, InventoryMovementType.Receipt, "L1");
        await _fixture.Batches.InsertAsync(new ProductBatch {
            ProductId = product.Id, CombinationId = "", WarehouseId = "w1", Number = "L1", NumberKey = "L1",
            Quantity = 5, StockRevision = 1
        });

        await _fixture.Apply(product);

        Assert.AreEqual(5, _fixture.BatchesOf(product).Single().Quantity);
        Assert.AreEqual(5, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Repair_SerialShipmentEntryLost_ReplaysShippedWithOrder()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber);
        await _fixture.Apply(product, UnitTestFixture.Change(1, InventoryMovementType.Receipt) with {
            SerialNumbers = ["A1"]
        });
        var shipment = new Shipment { OrderId = "o1" };
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "si1", OrderItemId = "oi1" });
        await _fixture.Shipments.InsertAsync(shipment);
        await LostEntry(product, 2, -1, InventoryMovementType.Shipment, serialNumbers: ["A1"],
            objectType: nameof(Shipment), objectId: shipment.Id, positionId: "si1");

        await _fixture.Apply(product);

        var serial = Serial(product, "A1");
        Assert.AreEqual(SerialNumberStatus.Shipped, serial.StatusId);
        Assert.AreEqual("o1", serial.OrderId);
        Assert.AreEqual("oi1", serial.OrderItemId);
        Assert.AreEqual(2, serial.StockRevision);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task RecomputeSums_ReplaysLostUnitWrite()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch);
        await LostEntry(product, 1, 5, InventoryMovementType.Receipt, "L1");

        await _fixture.Ledger.RecomputeSums(_fixture.Stored(product));

        Assert.AreEqual(5, _fixture.BatchesOf(product).Single().Quantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task StartTracking_WithStock_WritesMarkerPerBucket_PoolEqualsStock()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.SerialNumber, ("w1", null, 3),
            ("w2", null, 0));
        var revision = _fixture.Stored(product).StockRevision;

        await _fixture.Ledger.StartTracking(_fixture.Stored(product), "c1");

        var marker = _fixture.Entries(product).Last();
        Assert.AreEqual(InventoryMovementType.TrackingStarted, marker.MovementTypeId);
        Assert.AreEqual(0, marker.InQty);
        Assert.AreEqual(0, marker.OutQty);
        Assert.AreEqual("w1", marker.WarehouseId);
        Assert.AreEqual("pool 3", marker.Comments);
        Assert.AreEqual("c1", marker.CustomerId);
        Assert.AreEqual(1, _fixture.Entries(product).Count(x => x.MovementTypeId == InventoryMovementType.TrackingStarted));
        Assert.AreEqual(revision + 1, _fixture.Stored(product).StockRevision);
        Assert.AreEqual(3, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task StockCount_Serial_ConfirmedUnknownWithinPoolBeyondPoolMissing()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber, ("", null, 2));
        await _fixture.Apply(product, UnitTestFixture.Change(2, InventoryMovementType.Receipt) with {
            SerialNumbers = ["A", "B"]
        });

        var errors = await _fixture.Apply(product, Counted("", "A", "c", "D", "E"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Missing, Serial(product, "B").StatusId);
        Assert.AreEqual(4, _fixture.SerialsOf(product).Count(x => x.StatusId == SerialNumberStatus.InStock));
        Assert.AreEqual(4, _fixture.Stored(product).StockQuantity);
        var entries = _fixture.Entries(product);
        var assignment = entries.Single(x => x.MovementTypeId == InventoryMovementType.TrackingAssignment);
        CollectionAssert.AreEqual(new[] { "c", "D" }, assignment.SerialNumbers.ToArray());
        var found = entries.Single(x => x.MovementTypeId == InventoryMovementType.StockCount && x.InQty > 0);
        CollectionAssert.AreEqual(new[] { "E" }, found.SerialNumbers.ToArray());
        var missing = entries.Single(x => x.MovementTypeId == InventoryMovementType.StockCount && x.OutQty > 0);
        Assert.AreEqual(1, missing.OutQty);
        CollectionAssert.AreEqual(new[] { "B" }, missing.SerialNumbers.ToArray());
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task StockCount_Serial_ScannedInOtherWarehouse_Refused()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.SerialNumber);
        await _fixture.Apply(product, UnitTestFixture.Change(1, InventoryMovementType.Receipt, "w2") with {
            SerialNumbers = ["A1"]
        });

        var errors = await _fixture.Apply(product, Counted("w1", "A1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialInOtherWarehouse }, errors.ToArray());
    }

    [TestMethod]
    public async Task StockCount_Serial_PoolRestBookedOut()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber, ("", null, 3));

        var errors = await _fixture.Apply(product, Counted(""));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task StockCount_Serial_AppliedTwice_SecondTimeNoChange()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber, ("", null, 1));
        await _fixture.Apply(product, UnitTestFixture.Change(1, InventoryMovementType.Receipt) with {
            SerialNumbers = ["A"]
        });
        await _fixture.Apply(product, Counted("", "B"));
        var entries = _fixture.Entries(product).Count;

        var errors = await _fixture.Apply(product, Counted("", "B"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(entries, _fixture.Entries(product).Count);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }
}
