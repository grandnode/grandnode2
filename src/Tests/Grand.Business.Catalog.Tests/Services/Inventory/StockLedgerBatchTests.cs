using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class StockLedgerBatchTests
{
    private static readonly DateTime January = new(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc);
    private UnitTestFixture _fixture;

    [TestInitialize]
    public void Init()
    {
        _fixture = new UnitTestFixture();
    }

    private static Product Single(BackorderMode backorderMode = BackorderMode.NoBackorders) => new() {
        ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1", BackorderModeId = backorderMode
    };

    private static Product Multiple() => new() {
        ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true
    };

    private static StockChange Receipt(int quantity, string batch, DateTime? expiry = null, decimal? cost = null,
        string warehouseId = "w1", IList<CustomAttribute> attributes = null) =>
        UnitTestFixture.Change(quantity, InventoryMovementType.Receipt, warehouseId, attributes) with {
            BatchNumber = batch, ExpiryDateUtc = expiry, UnitCost = cost
        };

    private static StockChange Move(int quantity, InventoryMovementType type, string batch, string warehouseId = "w1") =>
        UnitTestFixture.Change(quantity, type, warehouseId) with { BatchNumber = batch };

    [TestMethod]
    public async Task Receipt_NewBatch_CreatesRecordWithExpiryAndCost()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch);

        var errors = await _fixture.Apply(product, Receipt(10, "L1", January, 2.5m));

        Assert.AreEqual(0, errors.Count);
        var batch = _fixture.BatchesOf(product).Single();
        Assert.AreEqual(10, batch.Quantity);
        Assert.AreEqual("L1", batch.Number);
        Assert.AreEqual("L1", batch.NumberKey);
        Assert.AreEqual(January, batch.ExpiryDateUtc);
        Assert.AreEqual(2.5m, batch.UnitCost);
        Assert.AreEqual("w1", batch.WarehouseId);
        Assert.AreEqual("store1", batch.StoreId);
        Assert.AreEqual("d1", batch.ReceiptDocumentId);
        Assert.AreEqual(_fixture.Entries(product).Last().StockRevision, batch.StockRevision);
        var entry = _fixture.Entries(product).Last();
        Assert.AreEqual("L1", entry.BatchNumber);
        Assert.AreEqual(January, entry.ExpiryDateUtc);
        Assert.AreEqual(2.5m, entry.UnitCost);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Receipt_ExistingBatch_AddsQuantity_KeepsExpiry()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch);
        await _fixture.Apply(product, Receipt(10, "L1", January, 2.5m));

        var errors = await _fixture.Apply(product, Receipt(5, " l1 "));

        Assert.AreEqual(0, errors.Count);
        var batch = _fixture.BatchesOf(product).Single();
        Assert.AreEqual(15, batch.Quantity);
        Assert.AreEqual(January, batch.ExpiryDateUtc);
        Assert.AreEqual(15, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Receipt_ExistingBatchOtherExpiry_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch);
        await _fixture.Apply(product, Receipt(10, "L1", January));
        var entries = _fixture.Entries(product).Count;

        var errors = await _fixture.Apply(product, Receipt(5, "L1", January.AddDays(1)));

        CollectionAssert.AreEqual(new[] { UnitErrors.BatchExpiryMismatch }, errors.ToArray());
        Assert.AreEqual(entries, _fixture.Entries(product).Count);
        Assert.AreEqual(10, _fixture.BatchesOf(product).Single().Quantity);
        Assert.AreEqual(10, _fixture.Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task Out_ByBatch_MoreThanBatch_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 5));
        await _fixture.Apply(product, Receipt(3, "L1"));

        var errors = await _fixture.Apply(product, Move(-4, InventoryMovementType.Adjustment, "L1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.BatchQuantityExceeded }, errors.ToArray());
        Assert.AreEqual(3, _fixture.BatchesOf(product).Single().Quantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Out_UnknownBatch_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 5));

        var errors = await _fixture.Apply(product, Move(-1, InventoryMovementType.Adjustment, "L9"));

        CollectionAssert.AreEqual(new[] { UnitErrors.BatchNotFound }, errors.ToArray());
    }

    [TestMethod]
    public async Task Out_WithoutBatch_BeyondPool_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 2));
        await _fixture.Apply(product, Receipt(4, "L1"));

        var errors = await _fixture.Apply(product, Move(-3, InventoryMovementType.Adjustment, null));

        CollectionAssert.AreEqual(new[] { UnitErrors.PoolExceeded }, errors.ToArray());
        Assert.AreEqual(6, _fixture.Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task Out_WithoutBatch_WithinPool_Applied()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 2));
        await _fixture.Apply(product, Receipt(4, "L1"));

        var errors = await _fixture.Apply(product, Move(-2, InventoryMovementType.Adjustment, null));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(4, _fixture.Stored(product).StockQuantity);
        Assert.AreEqual(4, _fixture.BatchesOf(product).Single().Quantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Shipment_WithoutBatch_BeyondPool_BackordersAllowed_GoesNegative()
    {
        var product = await _fixture.Stocked(Single(BackorderMode.AllowQtyBelowZero), InventoryTrackingMode.Batch,
            ("w1", null, 1));
        await _fixture.Apply(product, Receipt(4, "L1"));

        var errors = await _fixture.Apply(product, Move(-2, InventoryMovementType.Shipment, null));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(3, _fixture.Stored(product).StockQuantity);
        Assert.AreEqual(4, _fixture.BatchesOf(product).Single().Quantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Shipment_WithoutBatch_BeyondPool_NoBackorders_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 1));
        await _fixture.Apply(product, Receipt(4, "L1"));

        var errors = await _fixture.Apply(product, Move(-2, InventoryMovementType.Shipment, null));

        CollectionAssert.AreEqual(new[] { UnitErrors.PoolExceeded }, errors.ToArray());
    }

    [TestMethod]
    public async Task Receipt_AfterBackorderedPool_Applied()
    {
        var product = await _fixture.Stocked(Single(BackorderMode.AllowQtyBelowZero), InventoryTrackingMode.Batch);
        await _fixture.Apply(product, Move(-2, InventoryMovementType.Shipment, null));

        var errors = await _fixture.Apply(product, Receipt(4, "L1"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(2, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Transfer_PartOfBatch_CreatesTargetRecordSameExpiryAndCost()
    {
        var product = await _fixture.Stocked(Multiple(), InventoryTrackingMode.Batch);
        await _fixture.Apply(product, Receipt(10, "L1", January, 2.5m));

        var errors = await _fixture.Apply(product, Move(-4, InventoryMovementType.TransferOut, "L1"),
            Move(4, InventoryMovementType.TransferIn, "L1", "w2"));

        Assert.AreEqual(0, errors.Count);
        var batches = _fixture.BatchesOf(product);
        Assert.AreEqual(6, batches.Single(x => x.WarehouseId == "w1").Quantity);
        var target = batches.Single(x => x.WarehouseId == "w2");
        Assert.AreEqual(4, target.Quantity);
        Assert.AreEqual(January, target.ExpiryDateUtc);
        Assert.AreEqual(2.5m, target.UnitCost);
        Assert.AreEqual("", target.StoreId);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Assignment_FromPool_MovesQuantityToBatch_StockUnchanged()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 10));

        var errors = await _fixture.Apply(product,
            Move(6, InventoryMovementType.TrackingAssignment, "L2") with { ExpiryDateUtc = January });

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(10, _fixture.Stored(product).StockQuantity);
        var batch = _fixture.BatchesOf(product).Single();
        Assert.AreEqual(6, batch.Quantity);
        Assert.AreEqual(January, batch.ExpiryDateUtc);
        var entry = _fixture.Entries(product).Last();
        Assert.AreEqual(InventoryMovementType.TrackingAssignment, entry.MovementTypeId);
        Assert.AreEqual(6, entry.InQty);
        Assert.AreEqual(6, entry.OutQty);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Assignment_BeyondPool_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 3));

        var errors = await _fixture.Apply(product, Move(4, InventoryMovementType.TrackingAssignment, "L2"));

        CollectionAssert.AreEqual(new[] { UnitErrors.PoolExceeded }, errors.ToArray());
        Assert.AreEqual(0, _fixture.BatchesOf(product).Count);
    }

    [TestMethod]
    public async Task Assignment_WithoutBatch_BatchRequired()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch, ("w1", null, 3));

        var errors = await _fixture.Apply(product, Move(1, InventoryMovementType.TrackingAssignment, null));

        CollectionAssert.AreEqual(new[] { UnitErrors.BatchRequired }, errors.ToArray());
    }

    [TestMethod]
    public async Task Batch_QuantityReachesZero_RecordKept()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.Batch);
        await _fixture.Apply(product, Receipt(3, "L1"));

        await _fixture.Apply(product, Move(-3, InventoryMovementType.Adjustment, "L1"));

        Assert.AreEqual(0, _fixture.BatchesOf(product).Single().Quantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Combination_MultiWarehouse_BatchPerCombinationRow()
    {
        var product = await _fixture.Stocked(StockTestFixture.WithCombinations(true, 0, 0),
            InventoryTrackingMode.Batch);

        var errors = await _fixture.Apply(product,
            Receipt(2, "L1", warehouseId: "w1", attributes: StockTestFixture.Attributes("v1")),
            Receipt(3, "L1", warehouseId: "w2", attributes: StockTestFixture.Attributes("v2")));

        Assert.AreEqual(0, errors.Count);
        var stored = _fixture.Stored(product);
        var batches = _fixture.BatchesOf(product);
        Assert.AreEqual(2, batches.Count);
        var v1 = stored.ProductAttributeCombinations.First();
        Assert.AreEqual(2, batches.Single(x => x.CombinationId == v1.Id && x.WarehouseId == "w1").Quantity);
        Assert.AreEqual(StockTestFixture.Attributes("v1")[0].Value,
            batches.Single(x => x.CombinationId == v1.Id).Attributes.Single().Value);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task UntrackedProduct_NoUnitRepositoryCalls()
    {
        var batches = new Mock<IRepository<ProductBatch>>(MockBehavior.Strict);
        var serials = new Mock<IRepository<ProductSerialNumber>>(MockBehavior.Strict);
        var ledger = StockTestFixture.Ledger(_fixture.Products, _fixture.Journal, batches: batches.Object,
            serials: serials.Object);
        var product = Single();
        await _fixture.Products.InsertAsync(product);

        var errors = await ledger.Apply(_fixture.Stored(product), [Receipt(3, "L1")]);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(3, _fixture.Stored(product).StockQuantity);
    }
}
