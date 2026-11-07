using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class StockLedgerSerialTests
{
    private static readonly DateTime Warranty = new(2028, 11, 7, 0, 0, 0, DateTimeKind.Utc);
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

    private static StockChange Numbers(int quantity, InventoryMovementType type, string warehouseId,
        params string[] numbers) =>
        UnitTestFixture.Change(quantity, type, warehouseId) with { SerialNumbers = numbers };

    private async Task<Shipment> Shipment()
    {
        var shipment = new Shipment { OrderId = "o1", CreatedOnUtc = new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc) };
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "si1", OrderItemId = "oi1", WarrantyUntilUtc = Warranty });
        await _fixture.Shipments.InsertAsync(shipment);
        return shipment;
    }

    private static StockChange Shipping(Shipment shipment, int quantity, string warehouseId, params string[] numbers) =>
        new([], warehouseId, -quantity, false, InventoryMovementType.Shipment,
            new StockSource(nameof(Grand.Domain.Shipping.Shipment), shipment.Id, "si1")) { SerialNumbers = numbers };

    private static StockChange Reversal(Shipment shipment, int quantity, string warehouseId, params string[] numbers) =>
        new([], warehouseId, quantity, false, InventoryMovementType.ShipmentReversal,
            new StockSource(nameof(Grand.Domain.Shipping.Shipment), shipment.Id, "si1")) { SerialNumbers = numbers };

    private ProductSerialNumber Serial(Product product, string number) =>
        _fixture.SerialsOf(product).Single(x => x.NumberKey == UnitNumbers.Key(number));

    private async Task<Product> Received(Product product, string warehouseId, params string[] numbers)
    {
        product = await _fixture.Stocked(product, InventoryTrackingMode.SerialNumber);
        Assert.AreEqual(0, (await _fixture.Apply(product,
            Numbers(numbers.Length, InventoryMovementType.Receipt, warehouseId, numbers))).Count);
        return product;
    }

    [TestMethod]
    public async Task Receipt_Numbers_CreatesInStockRecords()
    {
        var product = await Received(Single(), "", "A1", "A2");

        var serials = _fixture.SerialsOf(product);
        Assert.AreEqual(2, serials.Count);
        Assert.IsTrue(serials.All(x => x.StatusId == SerialNumberStatus.InStock && x.WarehouseId == "w1" &&
                                       x.StoreId == "store1" && x.ReceiptDocumentId == "d1"));
        CollectionAssert.AreEqual(new[] { "A1", "A2" }, _fixture.Entries(product).Last().SerialNumbers.ToArray());
        Assert.AreEqual(2, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Adjustment_NumberInOtherCase_JournalNamesTheReceivedSpelling()
    {
        var product = await Received(Single(), "", "A1");

        await _fixture.Apply(product, Numbers(-1, InventoryMovementType.Adjustment, "", " a1 "));

        CollectionAssert.AreEqual(new[] { "A1" }, _fixture.Entries(product).Last().SerialNumbers.ToArray());
    }

    [TestMethod]
    public async Task Receipt_NumberInStock_Refused()
    {
        var product = await Received(Single(), "", "A1");

        var errors = await _fixture.Apply(product, Numbers(1, InventoryMovementType.Receipt, "", "a1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialInStock }, errors.ToArray());
        Assert.AreEqual(1, _fixture.SerialsOf(product).Count);
    }

    [TestMethod]
    public async Task Receipt_ScrappedNumber_ComesBack()
    {
        var product = await Received(Single(), "", "A1");
        await _fixture.Apply(product, Numbers(-1, InventoryMovementType.Adjustment, "", "A1"));

        var errors = await _fixture.Apply(product, Numbers(1, InventoryMovementType.Receipt, "", "A1"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.InStock, Serial(product, "A1").StatusId);
        Assert.AreEqual(1, _fixture.SerialsOf(product).Count);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Receipt_SameNumberDifferentCase_Duplicate_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber);

        var errors = await _fixture.Apply(product, Numbers(2, InventoryMovementType.Receipt, "", "a1", "A1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialDuplicate }, errors.ToArray());
        Assert.AreEqual(0, _fixture.SerialsOf(product).Count);
    }

    [TestMethod]
    public async Task Receipt_MoreNumbersThanQuantity_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber);

        var errors = await _fixture.Apply(product, Numbers(1, InventoryMovementType.Receipt, "", "A1", "A2"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialCountMismatch }, errors.ToArray());
    }

    [TestMethod]
    public async Task Shipment_InStockNumber_ShippedWithOrderFields()
    {
        var product = await Received(Single(), "", "A1", "A2");
        var shipment = await Shipment();

        var errors = await _fixture.Apply(product, Shipping(shipment, 1, "w9", "A1"));

        Assert.AreEqual(0, errors.Count);
        var serial = Serial(product, "A1");
        Assert.AreEqual(SerialNumberStatus.Shipped, serial.StatusId);
        Assert.AreEqual("o1", serial.OrderId);
        Assert.AreEqual("oi1", serial.OrderItemId);
        Assert.AreEqual(shipment.Id, serial.ShipmentId);
        //the repository stamps the creation date when the shipment is inserted
        Assert.AreEqual(_fixture.Shipments.Table.First(x => x.Id == shipment.Id).CreatedOnUtc, serial.ShippedOnUtc);
        Assert.AreEqual(Warranty, serial.WarrantyUntilUtc);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Shipment_NewNumberWithinPool_AssignedAndShipped()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber, ("", null, 2));
        var shipment = await Shipment();

        var errors = await _fixture.Apply(product, Shipping(shipment, 1, "", "N1"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Shipped, Serial(product, "N1").StatusId);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Shipment_NewNumberPoolEmpty_Refused()
    {
        var product = await Received(Single(), "", "A1");
        var shipment = await Shipment();

        var errors = await _fixture.Apply(product, Shipping(shipment, 1, "", "N1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.PoolExceeded }, errors.ToArray());
        Assert.AreEqual(1, _fixture.SerialsOf(product).Count);
    }

    [TestMethod]
    public async Task Shipment_NumberInOtherWarehouse_Refused()
    {
        var product = await Received(Multiple(), "w2", "A1");
        var shipment = await Shipment();

        var errors = await _fixture.Apply(product, Shipping(shipment, 1, "w1", "A1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialInOtherWarehouse }, errors.ToArray());
    }

    [TestMethod]
    public async Task Shipment_ShippedNumber_Refused()
    {
        var product = await Received(Single(), "", "A1", "A2");
        var shipment = await Shipment();
        await _fixture.Apply(product, Shipping(shipment, 1, "", "A1"));

        var errors = await _fixture.Apply(product, Shipping(shipment, 1, "", "A1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialShipped }, errors.ToArray());
    }

    [TestMethod]
    public async Task Shipment_FewerNumbersThanQuantity_Refused()
    {
        var product = await Received(Single(), "", "A1", "A2");
        var shipment = await Shipment();

        var errors = await _fixture.Apply(product, Shipping(shipment, 2, "", "A1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialCountMismatch }, errors.ToArray());
    }

    [TestMethod]
    public async Task Reversal_ReturnsSameNumbers_InStock_OrderFieldsCleared()
    {
        var product = await Received(Single(), "", "A1", "A2");
        var shipment = await Shipment();
        await _fixture.Apply(product, Shipping(shipment, 2, "", "A1", "A2"));

        var errors = await _fixture.Apply(product, Reversal(shipment, 2, "", "A1", "A2"));

        Assert.AreEqual(0, errors.Count);
        var serial = Serial(product, "A1");
        Assert.AreEqual(SerialNumberStatus.InStock, serial.StatusId);
        Assert.IsNull(serial.OrderId);
        Assert.IsNull(serial.ShipmentId);
        Assert.IsNull(serial.WarrantyUntilUtc);
        Assert.AreEqual(2, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Reversal_NumberOfOtherShipment_LeftOut()
    {
        var product = await Received(Single(), "", "A1");
        var shipment = await Shipment();
        var other = await Shipment();
        await _fixture.Apply(product, Shipping(shipment, 1, "", "A1"));

        var errors = await _fixture.Apply(product, Reversal(other, 1, "", "A1"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Shipped, Serial(product, "A1").StatusId);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Reversal_NumberReturnedSince_LeftOutWithItsUnit_RestGivenBack()
    {
        var product = await Received(Single(), "", "A1", "A2");
        var shipment = await Shipment();
        await _fixture.Apply(product, Shipping(shipment, 2, "", "A1", "A2"));
        await _fixture.Apply(product, Numbers(1, InventoryMovementType.ReturnReceipt, "", "A1"));

        var errors = await _fixture.Apply(product, Reversal(shipment, 2, "", "A1", "A2"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.InStock, Serial(product, "A1").StatusId);
        Assert.AreEqual(SerialNumberStatus.InStock, Serial(product, "A2").StatusId);
        Assert.AreEqual(2, _fixture.Stored(product).StockQuantity);
        CollectionAssert.AreEqual(new[] { "A2" }, _fixture.Entries(product).Last().SerialNumbers.ToArray());
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Transfer_Numbers_MoveWarehouse()
    {
        var product = await Received(Multiple(), "w1", "A1", "A2");

        var errors = await _fixture.Apply(product, Numbers(-1, InventoryMovementType.TransferOut, "w1", "A2"),
            Numbers(1, InventoryMovementType.TransferIn, "w2", "A2"));

        Assert.AreEqual(0, errors.Count);
        var serial = Serial(product, "A2");
        Assert.AreEqual(SerialNumberStatus.InStock, serial.StatusId);
        Assert.AreEqual("w2", serial.WarehouseId);
        Assert.AreEqual("", serial.StoreId);
        Assert.AreEqual("w1", Serial(product, "A1").WarehouseId);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task TransferIn_WithoutTransferOut_Refused()
    {
        var product = await Received(Multiple(), "w1", "A1");

        var errors = await _fixture.Apply(product, Numbers(1, InventoryMovementType.TransferIn, "w2", "A1"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialInStock }, errors.ToArray());
    }

    [TestMethod]
    public async Task AdjustmentOut_Scrapped_StockCountOut_Missing()
    {
        var product = await Received(Single(), "", "A1", "A2");

        var errors = await _fixture.Apply(product, Numbers(-1, InventoryMovementType.Adjustment, "", "A1"),
            Numbers(-1, InventoryMovementType.StockCount, "", "A2"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Scrapped, Serial(product, "A1").StatusId);
        Assert.AreEqual(SerialNumberStatus.Missing, Serial(product, "A2").StatusId);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task AdjustmentOut_NumberNotInStock_Refused()
    {
        var product = await Received(Single(), "", "A1");

        var errors = await _fixture.Apply(product, Numbers(-1, InventoryMovementType.Adjustment, "", "X9"));

        CollectionAssert.AreEqual(new[] { UnitErrors.SerialNotInStock }, errors.ToArray());
    }

    [TestMethod]
    public async Task Assignment_NewNumbers_FromPool()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber, ("", null, 3));

        var errors = await _fixture.Apply(product, Numbers(2, InventoryMovementType.TrackingAssignment, "", "B1", "B2"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(2, _fixture.SerialsOf(product).Count(x => x.StatusId == SerialNumberStatus.InStock));
        Assert.AreEqual(3, _fixture.Stored(product).StockQuantity);
        var entry = _fixture.Entries(product).Last();
        Assert.AreEqual(2, entry.InQty);
        Assert.AreEqual(2, entry.OutQty);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Assignment_BeyondPool_Refused()
    {
        var product = await _fixture.Stocked(Single(), InventoryTrackingMode.SerialNumber, ("", null, 1));

        var errors = await _fixture.Apply(product, Numbers(2, InventoryMovementType.TrackingAssignment, "", "B1", "B2"));

        CollectionAssert.AreEqual(new[] { UnitErrors.PoolExceeded }, errors.ToArray());
        Assert.AreEqual(0, _fixture.SerialsOf(product).Count);
    }

    [TestMethod]
    public async Task ReturnScrapped_ShippedToScrapped_NoStockChange()
    {
        var product = await Received(Single(), "", "A1", "A2");
        var shipment = await Shipment();
        await _fixture.Apply(product, Shipping(shipment, 1, "", "A1"));

        var errors = await _fixture.Apply(product, Numbers(0, InventoryMovementType.ReturnScrapped, "", "A1"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Scrapped, Serial(product, "A1").StatusId);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        var entry = _fixture.Entries(product).Last();
        Assert.AreEqual(InventoryMovementType.ReturnScrapped, entry.MovementTypeId);
        Assert.AreEqual(0, entry.InQty);
        Assert.AreEqual(0, entry.OutQty);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task ReturnReceipt_ShippedNumber_BackInStock()
    {
        var product = await Received(Single(), "", "A1");
        var shipment = await Shipment();
        await _fixture.Apply(product, Shipping(shipment, 1, "", "A1"));

        var errors = await _fixture.Apply(product, Numbers(1, InventoryMovementType.ReturnReceipt, "", "A1"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.InStock, Serial(product, "A1").StatusId);
        Assert.IsNull(Serial(product, "A1").OrderId);
        _fixture.AssertInStep(product);
    }
}
