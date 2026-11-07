using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
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
    private InventoryDocumentService _service;

    [TestInitialize]
    public void Init()
    {
        var settingService = new Mock<ISettingService>();
        settingService.Setup(x => x.LoadSetting<InventorySettings>(It.IsAny<string>())).ReturnsAsync(() => _settings);
        _service = new InventoryDocumentService(_documents, _fixture.Products, _fixture.Journal,
            StockTestFixture.Warehouses(), new MongoResourceLockTest(), settingService.Object,
            new Mock<IMediator>().Object, _fixture.Ledger, new Mock<IMerchandiseReturnService>().Object,
            new Mock<IOrderService>().Object);
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

        CollectionAssert.Contains((await _service.Validate(document)).ToList(), StockErrors.StockNotTracked);
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
