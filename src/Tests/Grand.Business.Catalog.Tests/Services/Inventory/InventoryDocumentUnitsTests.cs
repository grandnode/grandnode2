using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class InventoryDocumentUnitsTests
{
    private static readonly DateTime January = new(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc);
    private readonly IRepository<InventoryDocument> _documents = new MongoDBRepositoryTest<InventoryDocument>();
    private readonly UnitTestFixture _fixture = new();
    private readonly InventorySettings _settings = new();
    private readonly Mock<IMerchandiseReturnService> _merchandiseReturns = new();
    private readonly Mock<IOrderService> _orders = new();
    private readonly Mock<IShipmentService> _shipments = new();
    private InventoryDocumentService _service;

    [TestInitialize]
    public void Init()
    {
        var settingService = new Mock<ISettingService>();
        settingService.Setup(x => x.LoadSetting<InventorySettings>(It.IsAny<string>())).ReturnsAsync(() => _settings);
        _service = new InventoryDocumentService(_documents, _fixture.Products, _fixture.Journal,
            StockTestFixture.Warehouses(), new MongoResourceLockTest(), settingService.Object,
            new Mock<IMediator>().Object, _fixture.Ledger, _merchandiseReturns.Object, _orders.Object,
            StockTestFixture.UnitService(_fixture.Batches, _fixture.Serials), _shipments.Object);
    }

    /// <summary>
    ///     An order of the product from w1, shipped with the serial numbers given, and a return of
    ///     <paramref name="returned" /> units of it
    /// </summary>
    private async Task<MerchandiseReturn> ShippedAndReturned(Product product, int shipped, int returned,
        params string[] serialNumbers)
    {
        var orderItem = new OrderItem { ProductId = product.Id, WarehouseId = "w1", Quantity = shipped };
        var order = new Order { OrderItems = { orderItem } };
        var shipment = new Shipment { OrderId = order.Id };
        shipment.ShipmentItems.Add(new ShipmentItem {
            OrderItemId = orderItem.Id, ProductId = product.Id, Quantity = shipped, WarehouseId = "w1",
            SerialNumbers = serialNumbers
        });
        await _fixture.Shipments.InsertAsync(shipment);
        if (serialNumbers.Length > 0)
            Assert.AreEqual(0, (await _fixture.Apply(product, new StockChange([], "w1", -shipped, false,
                InventoryMovementType.Shipment,
                new StockSource(nameof(Shipment), shipment.Id, shipment.ShipmentItems.First().Id)) {
                SerialNumbers = serialNumbers
            })).Count);
        _shipments.Setup(x => x.GetShipmentsByOrder(order.Id)).ReturnsAsync([shipment]);

        var merchandiseReturn = new MerchandiseReturn {
            OrderId = order.Id, StoreId = "store1", MerchandiseReturnStatus = MerchandiseReturnStatus.Accepted,
            MerchandiseReturnItems = { new MerchandiseReturnItem { OrderItemId = orderItem.Id, Quantity = returned } }
        };
        _orders.Setup(x => x.GetOrderById(order.Id)).ReturnsAsync(order);
        _merchandiseReturns.Setup(x => x.GetMerchandiseReturnById(merchandiseReturn.Id)).ReturnsAsync(merchandiseReturn);
        return merchandiseReturn;
    }

    private async Task Receive(Product product, params string[] serialNumbers) =>
        Assert.AreEqual(0, (await _service.Post((await Draft(InventoryDocumentType.Receipt,
            Line(product, serialNumbers.Length, null, serialNumbers))).Id, "s")).Count);

    [TestMethod]
    public async Task CreateStockCount_Batch_LinePerBatchAndPool()
    {
        var product = await Batched(2);
        await _service.Post((await Draft(InventoryDocumentType.Receipt, Line(product, 3, "L1"))).Id, "s");

        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");

        var lines = document.Lines.Where(x => x.ProductId == product.Id).ToList();
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(3, lines.Single(x => x.BatchNumber == "L1").ExpectedQuantity);
        Assert.AreEqual(2, lines.Single(x => string.IsNullOrEmpty(x.BatchNumber)).ExpectedQuantity);
    }

    [TestMethod]
    public async Task Post_StockCount_BatchLineDifferenceBooked()
    {
        var product = await Batched(2);
        await _service.Post((await Draft(InventoryDocumentType.Receipt, Line(product, 3, "L1"))).Id, "s");
        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");
        document.Lines.Single(x => x.BatchNumber == "L1").CountedQuantity = 1;
        document.Lines.Single(x => string.IsNullOrEmpty(x.BatchNumber)).CountedQuantity = 2;
        await _service.UpdateDraft(document);

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(1, _fixture.BatchesOf(product).Single().Quantity);
        Assert.AreEqual(3, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Validate_StockCount_PoolLineUp_Refused()
    {
        var product = await Batched(2);
        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");
        document.Lines.Single(x => x.ProductId == product.Id).CountedQuantity = 4;

        CollectionAssert.Contains((await _service.Validate(document)).ToList(), UnitErrors.PoolExceeded);
    }

    [TestMethod]
    public async Task CreateStockCount_Serial_OneLinePerBucket()
    {
        var product = await Serialized();
        await Receive(product, "A1", "A2");

        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");

        var line = document.Lines.Single(x => x.ProductId == product.Id);
        Assert.AreEqual(2, line.ExpectedQuantity);
        Assert.AreEqual(0, line.SerialNumbers.Count);
    }

    [TestMethod]
    public async Task Post_StockCount_SerialLine_UnscannedMissing()
    {
        var product = await Serialized();
        await Receive(product, "A1", "A2");
        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");
        var line = document.Lines.Single(x => x.ProductId == product.Id);
        line.SerialNumbers = ["A1"];
        line.CountedQuantity = 1;
        await _service.UpdateDraft(document);

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Missing, _fixture.SerialsOf(product).Single(x => x.NumberKey == "A2").StatusId);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Post_StockCount_SerialLineBookedBefore_NotCountedAgain()
    {
        var product = await Serialized();
        await Receive(product, "A1", "A2");
        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");
        var line = document.Lines.Single(x => x.ProductId == product.Id);
        line.SerialNumbers = ["A1"];
        line.CountedQuantity = 1;
        await _service.UpdateDraft(document);
        //the product is booked, then another product stops the posting: the document stays a draft
        Assert.AreEqual(0, (await _service.Post(document.Id, "staff")).Count);
        await _documents.UpdateOneAsync(x => x.Id == document.Id,
            UpdateBuilder<InventoryDocument>.Create().Set(x => x.StatusId, InventoryDocumentStatus.Draft));
        await Receive(product, "Y1");

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.InStock, _fixture.SerialsOf(product).Single(x => x.NumberKey == "Y1").StatusId);
        Assert.AreEqual(2, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task CreateReturnReceipt_Serial_PrefillsShippedNumbers()
    {
        var product = await Serialized();
        await Receive(product, "A1", "A2");
        var merchandiseReturn = await ShippedAndReturned(product, 2, 1, "A1", "A2");

        var (document, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");

        CollectionAssert.AreEqual(new[] { "A1", "A2" }, document.Lines.Single().SerialNumbers.ToArray());
    }

    [TestMethod]
    public async Task Post_ReturnReceipt_SerialRestock_InStockAgain()
    {
        var product = await Serialized();
        await Receive(product, "A1", "A2");
        var merchandiseReturn = await ShippedAndReturned(product, 2, 1, "A1", "A2");
        var (document, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");
        document.Lines.Single().SerialNumbers = ["A2"];
        await _service.UpdateDraft(document);

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.InStock, _fixture.SerialsOf(product).Single(x => x.NumberKey == "A2").StatusId);
        Assert.AreEqual(SerialNumberStatus.Shipped, _fixture.SerialsOf(product).Single(x => x.NumberKey == "A1").StatusId);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Post_ReturnReceipt_SerialScrap_ScrappedNoStock()
    {
        var product = await Serialized();
        await Receive(product, "A1");
        var merchandiseReturn = await ShippedAndReturned(product, 1, 1, "A1");
        var (document, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");
        document.Lines.Single().Condition = ReturnLineCondition.Scrap;
        await _service.UpdateDraft(document);

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Scrapped, _fixture.SerialsOf(product).Single().StatusId);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task ReturnReceipt_ShippedWithoutNumbers_RestocksToPool()
    {
        var product = await Serialized();
        var merchandiseReturn = await ShippedAndReturned(product, 1, 1);
        var (document, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        Assert.AreEqual(0, _fixture.SerialsOf(product).Count);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Validate_ReturnReceipt_NumberNotShippedForItem_Refused()
    {
        var product = await Serialized();
        await Receive(product, "A1", "B1");
        var merchandiseReturn = await ShippedAndReturned(product, 1, 1, "A1");
        var (document, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");
        document.Lines.Single().SerialNumbers = ["B1"];

        CollectionAssert.Contains((await _service.Validate(document)).ToList(), UnitErrors.SerialNotShipped);
    }

    private Task<Product> Batched(int pool = 0) => _fixture.Stocked(
        new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1" },
        InventoryTrackingMode.Batch, ("", null, pool));

    private Task<Product> Serialized(int pool = 0, bool multipleWarehouses = false) => _fixture.Stocked(
        new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = multipleWarehouses ? "" : "w1",
            UseMultipleWarehouses = multipleWarehouses
        }, InventoryTrackingMode.SerialNumber, (multipleWarehouses ? "w1" : "", null, pool));

    private Task<InventoryDocument> Draft(InventoryDocumentType type, params InventoryDocumentLine[] lines)
    {
        var document = new InventoryDocument {
            DocumentTypeId = type, WarehouseId = "w1", Comment = "c",
            TargetWarehouseId = type == InventoryDocumentType.Transfer ? "w2" : null
        };
        foreach (var line in lines)
            document.Lines.Add(line);
        return _service.CreateDraft(document);
    }

    private static InventoryDocumentLine Line(Product product, int quantity, string batch = null,
        params string[] serialNumbers) =>
        new() {
            ProductId = product.Id, Quantity = quantity, BatchNumber = batch, SerialNumbers = serialNumbers,
            UnitCost = 1m
        };

    [TestMethod]
    public async Task Post_BatchReceipt_CreatesBatchesPerLine()
    {
        var product = await Batched();
        var first = Line(product, 4, "L1");
        first.ExpiryDateUtc = January;
        var document = await Draft(InventoryDocumentType.Receipt, first, Line(product, 2, "L2"));

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        var batches = _fixture.BatchesOf(product);
        Assert.AreEqual(4, batches.Single(x => x.NumberKey == "L1").Quantity);
        Assert.AreEqual(January, batches.Single(x => x.NumberKey == "L1").ExpiryDateUtc);
        Assert.AreEqual(document.Id, batches.Single(x => x.NumberKey == "L1").ReceiptDocumentId);
        Assert.AreEqual(2, batches.Single(x => x.NumberKey == "L2").Quantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Validate_BatchReceiptWithoutNumber_BatchRequired()
    {
        var product = await Batched();
        var document = await Draft(InventoryDocumentType.Receipt, Line(product, 4));

        CollectionAssert.Contains((await _service.Validate(document)).ToList(), UnitErrors.BatchRequired);
    }

    [TestMethod]
    public async Task Validate_SerialReceiptCountMismatch_Refused()
    {
        var product = await Serialized();
        var document = await Draft(InventoryDocumentType.Receipt, Line(product, 2, null, "A1"));

        CollectionAssert.Contains((await _service.Validate(document)).ToList(), UnitErrors.SerialCountMismatch);
    }

    [TestMethod]
    public async Task Validate_SerialDuplicateAcrossLines_Refused()
    {
        var product = await Serialized();
        var document = await Draft(InventoryDocumentType.Receipt, Line(product, 1, null, "a1"),
            Line(product, 1, null, "A1"));

        CollectionAssert.Contains((await _service.Validate(document)).ToList(), UnitErrors.SerialDuplicate);
    }

    [TestMethod]
    public async Task Post_SerialAdjustmentOut_NumbersScrapped()
    {
        var product = await Serialized();
        await _service.Post((await Draft(InventoryDocumentType.Receipt, Line(product, 2, null, "A1", "A2"))).Id, "s");
        var document = await Draft(InventoryDocumentType.Adjustment, Line(product, -1, null, "A2"));

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(SerialNumberStatus.Scrapped,
            _fixture.SerialsOf(product).Single(x => x.NumberKey == "A2").StatusId);
        Assert.AreEqual(1, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Post_Transfer_BatchMovesWithExpiry()
    {
        var product = await _fixture.Stocked(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true
        }, InventoryTrackingMode.Batch);
        var receipt = Line(product, 5, "L1");
        receipt.ExpiryDateUtc = January;
        await _service.Post((await Draft(InventoryDocumentType.Receipt, receipt)).Id, "s");
        var document = await Draft(InventoryDocumentType.Transfer, Line(product, 3, "L1"));

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        var moved = _fixture.BatchesOf(product).Single(x => x.WarehouseId == "w2");
        Assert.AreEqual(3, moved.Quantity);
        Assert.AreEqual(January, moved.ExpiryDateUtc);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Post_Assignment_FillsPool_StockUnchanged_NumberPrz()
    {
        var product = await Serialized(3);
        var document = await Draft(InventoryDocumentType.Assignment, Line(product, 2, null, "S1", "S2"));

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        StringAssert.StartsWith(document.Number, "PRZ/");
        Assert.AreEqual(3, _fixture.Stored(product).StockQuantity);
        Assert.AreEqual(2, _fixture.SerialsOf(product).Count(x => x.StatusId == SerialNumberStatus.InStock));
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Post_Assignment_BeyondPool_StaysDraft()
    {
        var product = await Batched(2);
        var document = await Draft(InventoryDocumentType.Assignment, Line(product, 3, "L1"));

        var errors = await _service.Post(document.Id, "staff");

        CollectionAssert.AreEqual(new[] { UnitErrors.PoolExceeded }, errors.ToArray());
        Assert.AreEqual(InventoryDocumentStatus.Draft, (await _documents.GetByIdAsync(document.Id)).StatusId);
    }

    [TestMethod]
    public async Task Validate_AssignmentOfUntrackedProduct_Refused()
    {
        var product = await _fixture.Stocked(
            new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1" },
            InventoryTrackingMode.None, ("", null, 2));
        var document = await Draft(InventoryDocumentType.Assignment, Line(product, 1, "L1"));

        //the product keeps stock: what it lacks is batches or serial numbers to give it
        var errors = (await _service.Validate(document)).ToList();
        CollectionAssert.Contains(errors, InventoryDocumentErrors.UnitsNotTracked);
        CollectionAssert.DoesNotContain(errors, StockErrors.StockNotTracked);
    }

    [TestMethod]
    public async Task Post_UntrackedProduct_SameAsStage1()
    {
        var product = await _fixture.Stocked(
            new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1" },
            InventoryTrackingMode.None);
        var document = await Draft(InventoryDocumentType.Receipt, Line(product, 4));

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(4, _fixture.Stored(product).StockQuantity);
        var entry = _fixture.Entries(product).Last();
        Assert.IsNull(entry.BatchNumber);
        Assert.AreEqual(0, entry.SerialNumbers.Count);
        Assert.AreEqual(0, _fixture.BatchesOf(product).Count);
    }
}
