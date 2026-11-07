using Grand.Business.Catalog.Services.Inventory;
using Grand.Data;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
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
public class InventoryDocumentServiceTests
{
    private IRepository<InventoryDocument> _documents;
    private InventorySettings _inventorySettings;
    private IRepository<InventoryJournal> _journal;
    private IRepository<Product> _products;
    private InventoryDocumentService _service;

    [TestInitialize]
    public void Init()
    {
        _products = new MongoDBRepositoryTest<Product>();
        _journal = new MongoDBRepositoryTest<InventoryJournal>();
        _documents = new MongoDBRepositoryTest<InventoryDocument>();
        _inventorySettings = new InventorySettings();
        var settingService = new Mock<ISettingService>();
        settingService.Setup(x => x.LoadSetting<InventorySettings>(It.IsAny<string>()))
            .ReturnsAsync(() => _inventorySettings);
        _settingService = settingService.Object;
        _service = Service(new MongoResourceLockTest());
    }

    private ISettingService _settingService;

    private Mock<IMerchandiseReturnService> _merchandiseReturnService = new();
    private Mock<IOrderService> _orderService = new();

    private InventoryDocumentService Service(IResourceLock resourceLock) =>
        new(_documents, _products, _journal, StockTestFixture.Warehouses(), resourceLock, _settingService,
            new Mock<IMediator>().Object, StockTestFixture.Ledger(_products, _journal),
            _merchandiseReturnService.Object, _orderService.Object);

    /// <summary>
    ///     A return of two units of the product, ordered from w1
    /// </summary>
    private MerchandiseReturn Return(Product product, MerchandiseReturnStatus status = MerchandiseReturnStatus.Accepted)
    {
        var orderItem = new OrderItem { ProductId = product.Id, WarehouseId = "w1", Quantity = 2 };
        var order = new Order { OrderItems = { orderItem } };
        var merchandiseReturn = new MerchandiseReturn {
            OrderId = order.Id, StoreId = "store1", MerchandiseReturnStatus = status,
            MerchandiseReturnItems = { new MerchandiseReturnItem { OrderItemId = orderItem.Id, Quantity = 2 } }
        };
        _orderService.Setup(x => x.GetOrderById(order.Id)).ReturnsAsync(order);
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnById(merchandiseReturn.Id))
            .ReturnsAsync(merchandiseReturn);
        return merchandiseReturn;
    }

    [TestMethod]
    public async Task CreateStockCount_FillsExpectedFromStoredStock()
    {
        var single = await Product(p => p.WarehouseId = "w1");
        var multi = await MultiWarehouseProduct(4, 9);

        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");

        Assert.AreEqual(InventoryDocumentType.StockCount, document.DocumentTypeId);
        Assert.AreEqual(10, document.Lines.Single(x => x.ProductId == single.Id).ExpectedQuantity);
        Assert.AreEqual(4, document.Lines.Single(x => x.ProductId == multi.Id).ExpectedQuantity);
        Assert.IsTrue(document.Lines.All(x => x.CountedQuantity == null));
    }

    [TestMethod]
    public async Task CreateStockCount_SkipsProductsNotInWarehouse()
    {
        await Product(p => p.WarehouseId = "w2");
        await Product();
        var combinations = StockTestFixture.WithCombinations(false, 3, 4);
        combinations.WarehouseId = "w1";
        await _products.InsertAsync(combinations);

        var (document, _) = await _service.CreateStockCount("w1", [], [], "staff");

        Assert.AreEqual(2, document.Lines.Count);
        Assert.IsTrue(document.Lines.All(x => x.ProductId == combinations.Id));
        CollectionAssert.AreEquivalent(new int?[] { 3, 4 }, document.Lines.Select(x => x.ExpectedQuantity).ToArray());
    }

    [TestMethod]
    public async Task CreateStockCount_CategoryFilter()
    {
        var inCategory = await Product(p => {
            p.WarehouseId = "w1";
            p.ProductCategories.Add(new ProductCategory { CategoryId = "c1" });
        });
        await Product(p => p.WarehouseId = "w1");

        var (document, _) = await _service.CreateStockCount("w1", ["c1"], [], "staff");

        Assert.AreEqual(inCategory.Id, document.Lines.Single().ProductId);
    }

    [TestMethod]
    public async Task CreateReturnReceipt_RejectedReturn_Refused()
    {
        var product = await Product();
        var merchandiseReturn = Return(product, MerchandiseReturnStatus.Rejected);

        var (document, errors) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");

        Assert.IsNull(document);
        CollectionAssert.AreEqual(new[] { InventoryDocumentErrors.ReturnNotReceivable }, errors.ToArray());
    }

    [TestMethod]
    public async Task CreateReturnReceipt_PrefillsLinesAndWarehouse()
    {
        var product = await Product();
        var merchandiseReturn = Return(product);

        var (document, errors) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("w1", document.WarehouseId);
        Assert.AreEqual(merchandiseReturn.Id, document.MerchandiseReturnId);
        var line = document.Lines.Single();
        Assert.AreEqual(product.Id, line.ProductId);
        Assert.AreEqual(2, line.Quantity);
        Assert.AreEqual(merchandiseReturn.MerchandiseReturnItems[0].Id, line.MerchandiseReturnItemId);
    }

    [TestMethod]
    public async Task CreateReturnReceipt_Bundle_ExpandsChildren()
    {
        var child = await Product();
        var bundle = await Product(p => {
            p.ManageInventoryMethodId = ManageInventoryMethod.ManageStockByBundleProducts;
            p.BundleProducts.Add(new BundleProduct { ProductId = child.Id, Quantity = 3 });
        });
        var merchandiseReturn = Return(bundle);

        var (document, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");

        var line = document.Lines.Single();
        Assert.AreEqual(child.Id, line.ProductId);
        Assert.AreEqual(6, line.Quantity);
    }

    [TestMethod]
    public async Task CreateReturnReceipt_SecondReceipt_OnlyRemainingQuantity()
    {
        var product = await Product();
        var merchandiseReturn = Return(product);
        var (first, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");
        first.Lines.Single().Quantity = 1;
        await _service.UpdateDraft(first);

        var (second, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");

        Assert.AreEqual(1, second.Lines.Single().Quantity);
    }

    [TestMethod]
    public async Task Validate_ReturnReceiptOverReturnedQuantity_Error()
    {
        var product = await Product();
        var merchandiseReturn = Return(product);
        var (first, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");
        var (second, _) = await _service.CreateReturnReceipt(merchandiseReturn.Id, "staff");
        Assert.IsNull(second);

        first.Lines.Single().Quantity = 3;

        CollectionAssert.Contains((await _service.Validate(first)).ToList(),
            InventoryDocumentErrors.ReturnQuantityExceeded);
    }

    private Product Stored(Product product) => _products.Table.First(x => x.Id == product.Id);

    private async Task<InventoryDocument> Draft(InventoryDocument document)
    {
        await _documents.InsertAsync(document);
        return document;
    }

    private async Task<Product> MultiWarehouseProduct(int w1, int w2 = 0)
    {
        return await Product(p => {
            p.UseMultipleWarehouses = true;
            p.StockQuantity = w1 + w2;
            p.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = w1 });
            p.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = w2 });
        });
    }

    private void AssertJournalMatchesStock(Product product)
    {
        var stored = Stored(product);
        if (stored.UseMultipleWarehouses)
            foreach (var row in stored.ProductWarehouseInventory)
                Assert.AreEqual(row.StockQuantity,
                    StockTestFixture.JournalSum(_journal, product.Id, "", row.WarehouseId), row.WarehouseId);
        else
            Assert.AreEqual(stored.StockQuantity, StockTestFixture.JournalSum(_journal, product.Id, "", ""));
    }

    private async Task Opened(Product product)
    {
        //the history of a product that existed before the journal starts with its opening balance
        await StockTestFixture.Ledger(_products, _journal)
            .Reopen(Stored(product), ManageInventoryMethod.DontManageStock, false, null, "");
    }

    [TestMethod]
    public async Task Post_Receipt_AddsStockAndFreezes()
    {
        var product = await Product();
        await Opened(product);
        var document = await Draft(Document(InventoryDocumentType.Receipt, Line(product, 5, 2)));

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(15, Stored(product).StockQuantity);
        var posted = _documents.Table.Single();
        Assert.AreEqual(InventoryDocumentStatus.Posted, posted.StatusId);
        Assert.AreEqual("staff", posted.PostedBy);
        Assert.IsNotNull(posted.PostedOnUtc);
        var entry = _journal.Table.Single(x => x.MovementTypeId == InventoryMovementType.Receipt);
        Assert.AreEqual(document.Id, entry.ObjectId);
        Assert.AreEqual(document.Lines.Single().Id, entry.PositionId);
        Assert.AreEqual("staff", entry.CustomerId);
        AssertJournalMatchesStock(product);
    }

    [TestMethod]
    public async Task Post_Transfer_WritesOutAndInAndMovesStock()
    {
        var product = await MultiWarehouseProduct(6);
        await Opened(product);
        var document = Document(InventoryDocumentType.Transfer, Line(product, 4));
        document.TargetWarehouseId = "w2";
        await Draft(document);

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        var rows = Stored(product).ProductWarehouseInventory;
        Assert.AreEqual(2, rows.Single(x => x.WarehouseId == "w1").StockQuantity);
        Assert.AreEqual(4, rows.Single(x => x.WarehouseId == "w2").StockQuantity);
        Assert.AreEqual(6, Stored(product).StockQuantity);
        AssertJournalMatchesStock(product);
    }

    [TestMethod]
    public async Task Post_TransferShortOfAvailable_ErrorAndStaysDraft()
    {
        var product = await MultiWarehouseProduct(3);
        var document = Document(InventoryDocumentType.Transfer, Line(product, 4));
        document.TargetWarehouseId = "w2";
        await Draft(document);

        var errors = await _service.Post(document.Id, "staff");

        CollectionAssert.AreEqual(new[] { StockErrors.NotEnoughAvailable }, errors.ToArray());
        Assert.AreEqual(InventoryDocumentStatus.Draft, _documents.Table.Single().StatusId);
        Assert.AreEqual(3, Stored(product).StockQuantity);
        Assert.IsFalse(_journal.Table.Any(x => x.ObjectId == document.Id));
    }

    [TestMethod]
    public async Task Post_StockCount_SaleBetweenCreateAndPost_BooksDifferenceOnCurrentStock()
    {
        var product = await Product();
        await Opened(product);
        var document = await Draft(Document(InventoryDocumentType.StockCount,
            new InventoryDocumentLine { ProductId = product.Id, ExpectedQuantity = 10, CountedQuantity = 8 }));
        //three units shipped while the count was going on
        await StockTestFixture.Ledger(_products, _journal).Apply(Stored(product), [
            new StockChange([], "", -3, false, InventoryMovementType.Shipment, new StockSource("Shipment"))
        ]);

        await _service.Post(document.Id, "staff");

        Assert.AreEqual(5, Stored(product).StockQuantity);
        Assert.AreEqual(2, _journal.Table.Single(x => x.MovementTypeId == InventoryMovementType.StockCount).OutQty);
        AssertJournalMatchesStock(product);
    }

    [TestMethod]
    public async Task Post_DraftSavedWhilePosting_StopsBeforeBooking()
    {
        var product = await Product();
        var document = await Draft(Document(InventoryDocumentType.Adjustment, Line(product, 1)));
        var resourceLock = new Mock<IResourceLock>();
        resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IEnumerable<string> keys, CancellationToken _) =>
            {
                //another user saves the draft just as its first product is to be booked
                if (keys.Contains(ProductStockLock.Key(product.Id)))
                    await _documents.UpdateAsync(_documents.Table.Single(x => x.Id == document.Id));
                return new Mock<IAsyncDisposable>().Object;
            });

        var errors = await Service(resourceLock.Object).Post(document.Id, "staff");

        CollectionAssert.AreEqual(new[] { InventoryDocumentErrors.ChangedWhilePosting }, errors.ToArray());
        Assert.IsFalse(_journal.Table.Any(x => x.ObjectId == document.Id));
        Assert.AreEqual(InventoryDocumentStatus.Draft, _documents.Table.Single().StatusId);
    }

    [TestMethod]
    public async Task Validate_MoreLinesThanADocumentHolds_Rejected()
    {
        var product = await Product();
        var document = Document(InventoryDocumentType.Receipt,
            Enumerable.Range(0, InventoryDocumentService.MaxLines + 1).Select(_ => Line(product, 1)).ToArray());

        var errors = await _service.Validate(document);

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.TooManyLines);
    }

    [TestMethod]
    public async Task Post_StockCount_SameBucketTwice_Rejected()
    {
        var product = await Product();
        var document = await Draft(Document(InventoryDocumentType.StockCount,
            new InventoryDocumentLine { ProductId = product.Id, ExpectedQuantity = 10, CountedQuantity = 12 },
            new InventoryDocumentLine { ProductId = product.Id, ExpectedQuantity = 10, CountedQuantity = 12 }));

        var errors = await _service.Post(document.Id, "staff");

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.DuplicateLine);
        Assert.AreEqual(10, Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task Post_StockCount_EmptyCountedLine_Skipped()
    {
        var product = await Product();
        var document = await Draft(Document(InventoryDocumentType.StockCount,
            new InventoryDocumentLine { ProductId = product.Id, ExpectedQuantity = 10 }));

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(10, Stored(product).StockQuantity);
        Assert.IsFalse(_journal.Table.Any());
        Assert.AreEqual(InventoryDocumentStatus.Posted, _documents.Table.Single().StatusId);
    }

    [TestMethod]
    public async Task Post_ReturnReceipt_ScrapChangesNoStock()
    {
        var product = await Product();
        var document = await Draft(Document(InventoryDocumentType.ReturnReceipt,
            new InventoryDocumentLine { ProductId = product.Id, Quantity = 2, Condition = ReturnLineCondition.Restock },
            new InventoryDocumentLine { ProductId = product.Id, Quantity = 1, Condition = ReturnLineCondition.Scrap }));

        await _service.Post(document.Id, "staff");

        Assert.AreEqual(12, Stored(product).StockQuantity);
        Assert.AreEqual(2, _journal.Table.Single().InQty);
    }

    [TestMethod]
    public async Task Post_ResumedAfterPartialBooking_BooksEachLineOnce()
    {
        var first = await Product();
        var second = await Product();
        await Opened(first);
        await Opened(second);
        var document = await Draft(Document(InventoryDocumentType.Adjustment, Line(first, 2), Line(second, 3)));
        //a posting that stopped after booking the first line
        await StockTestFixture.Ledger(_products, _journal).Apply(Stored(first), [
            new StockChange([], "w1", 2, false, InventoryMovementType.Adjustment,
                new StockSource(nameof(InventoryDocument), document.Id, document.Lines[0].Id))
        ]);

        var errors = await _service.Post(document.Id, "staff");

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(12, Stored(first).StockQuantity);
        Assert.AreEqual(13, Stored(second).StockQuantity);
        AssertJournalMatchesStock(first);
        AssertJournalMatchesStock(second);
    }

    [TestMethod]
    public async Task Post_ManyProducts_HoldsTheDocumentKeyWithOneProductKeyAtATime()
    {
        var products = new List<Product>();
        for (var i = 0; i < 3; i++)
            products.Add(await Product());
        var document = await Draft(Document(InventoryDocumentType.Adjustment,
            products.Select(p => Line(p, 1)).ToArray()));
        var acquired = new List<string[]>();
        var resourceLock = new Mock<IResourceLock>();
        resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<string> keys, CancellationToken _) => acquired.Add(keys.ToArray()))
            .ReturnsAsync(new Mock<IAsyncDisposable>().Object);

        await Service(resourceLock.Object).Post(document.Id, "staff");

        var documentKey = $"InventoryDocument:{document.Id}";
        Assert.AreEqual(5, acquired.Count);
        Assert.IsTrue(acquired.All(x => x.Contains(documentKey)));
        CollectionAssert.AreEquivalent(products.Select(p => ProductStockLock.Key(p.Id)).ToArray(),
            acquired.SelectMany(x => x).Where(x => x != documentKey).ToArray());
        Assert.IsTrue(acquired.All(x => x.Length <= 2));
    }

    [TestMethod]
    public async Task Post_Twice_SecondRejected()
    {
        var product = await Product();
        var document = await Draft(Document(InventoryDocumentType.Receipt, Line(product, 5, 2)));
        await _service.Post(document.Id, "staff");

        var errors = await _service.Post(document.Id, "staff");

        CollectionAssert.AreEqual(new[] { InventoryDocumentErrors.Posted }, errors.ToArray());
        Assert.AreEqual(15, Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task Post_MixedSequence_JournalSumEqualsStoredStock()
    {
        var product = await MultiWarehouseProduct(10, 1);
        await Opened(product);
        var ledger = StockTestFixture.Ledger(_products, _journal);
        var receipt = await Draft(Document(InventoryDocumentType.Receipt, Line(product, 5, 3)));
        await _service.Post(receipt.Id, "staff");
        await ledger.Apply(Stored(product), [
            new StockChange([], "w1", -4, false, InventoryMovementType.Shipment, new StockSource("Shipment"))
        ]);
        await ledger.Apply(Stored(product), [
            new StockChange([], "w1", 4, false, InventoryMovementType.ShipmentReversal, new StockSource("Shipment"))
        ]);
        var adjustment = await Draft(Document(InventoryDocumentType.Adjustment, Line(product, -2)));
        await _service.Post(adjustment.Id, "staff");
        var count = await Draft(Document(InventoryDocumentType.StockCount,
            new InventoryDocumentLine { ProductId = product.Id, ExpectedQuantity = 13, CountedQuantity = 12 }));
        await _service.Post(count.Id, "staff");
        var transfer = Document(InventoryDocumentType.Transfer, Line(product, 6));
        transfer.TargetWarehouseId = "w2";
        await Draft(transfer);
        await _service.Post(transfer.Id, "staff");
        await ledger.Apply(Stored(product), [
            new StockChange([], "w2", 20, true, InventoryMovementType.ManualEdit, new StockSource("Product"))
        ]);

        var rows = Stored(product).ProductWarehouseInventory;
        Assert.AreEqual(6, rows.Single(x => x.WarehouseId == "w1").StockQuantity);
        Assert.AreEqual(20, rows.Single(x => x.WarehouseId == "w2").StockQuantity);
        AssertJournalMatchesStock(product);
    }

    private async Task<Product> Product(Action<Product> setup = null)
    {
        var product = new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 10 };
        setup?.Invoke(product);
        await _products.InsertAsync(product);
        return product;
    }

    private static InventoryDocument Document(InventoryDocumentType type, params InventoryDocumentLine[] lines)
    {
        var document = new InventoryDocument { DocumentTypeId = type, WarehouseId = "w1", Comment = "comment" };
        foreach (var line in lines)
            document.Lines.Add(line);
        return document;
    }

    private static InventoryDocumentLine Line(Product product, int quantity, decimal? unitCost = 1) =>
        new() { ProductId = product.Id, Quantity = quantity, UnitCost = unitCost };

    [TestMethod]
    public async Task CreateDraft_FirstOfYear_IsPrefixYear0001()
    {
        var document = await _service.CreateDraft(Document(InventoryDocumentType.Receipt));

        Assert.AreEqual($"PZ/{DateTime.UtcNow.Year}/0001", document.Number);
        Assert.AreEqual(InventoryDocumentStatus.Draft, document.StatusId);
        Assert.AreEqual("store1", document.StoreId);
    }

    [TestMethod]
    public async Task CreateDraft_Parallel_NumbersAreUnique()
    {
        var documents = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            Task.Run(() => _service.CreateDraft(Document(InventoryDocumentType.Adjustment)))));

        Assert.AreEqual(20, documents.Select(x => x.Number).Distinct().Count());
        CollectionAssert.AreEquivalent(Enumerable.Range(1, 20).ToArray(),
            documents.Select(x => x.Sequence).ToArray());
    }

    [TestMethod]
    public async Task CreateDraft_NewYear_StartsAgain()
    {
        await _documents.InsertAsync(new InventoryDocument
            { DocumentTypeId = InventoryDocumentType.Receipt, Year = DateTime.UtcNow.Year - 1, Sequence = 7 });

        var document = await _service.CreateDraft(Document(InventoryDocumentType.Receipt));

        Assert.AreEqual(1, document.Sequence);
    }

    [TestMethod]
    public async Task CreateDraft_UsesConfiguredPrefix()
    {
        _inventorySettings.StockCountNumberPrefix = "SC";

        var document = await _service.CreateDraft(Document(InventoryDocumentType.StockCount));

        Assert.AreEqual($"SC/{DateTime.UtcNow.Year}/0001", document.Number);
    }

    [TestMethod]
    public async Task Validate_ReceiptWithoutUnitCost_Error()
    {
        var product = await Product();

        var errors = await _service.Validate(Document(InventoryDocumentType.Receipt, Line(product, 2, null)));

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.UnitCostRequired);
    }

    [TestMethod]
    public async Task Validate_AdjustmentWithoutComment_Error()
    {
        var product = await Product();
        var document = Document(InventoryDocumentType.Adjustment, Line(product, -2));
        document.Comment = " ";

        var errors = await _service.Validate(document);

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.CommentRequired);
    }

    [TestMethod]
    public async Task Validate_TransferOfSingleWarehouseProduct_Error()
    {
        var product = await Product();
        var document = Document(InventoryDocumentType.Transfer, Line(product, 2));
        document.TargetWarehouseId = "w2";

        var errors = await _service.Validate(document);

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.TransferNeedsMultipleWarehouses);
    }

    [TestMethod]
    public async Task Validate_TransferToTheSameWarehouse_Error()
    {
        var product = await Product(p => p.UseMultipleWarehouses = true);
        var document = Document(InventoryDocumentType.Transfer, Line(product, 2));
        document.TargetWarehouseId = "w1";

        var errors = await _service.Validate(document);

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.TargetWarehouseInvalid);
    }

    [TestMethod]
    public async Task Validate_SingleWarehouseProductInOtherWarehouse_Error()
    {
        var product = await Product(p => p.WarehouseId = "w2");

        var errors = await _service.Validate(Document(InventoryDocumentType.Receipt, Line(product, 2)));

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.ProductInOtherWarehouse);
    }

    [TestMethod]
    public async Task Validate_ProductWithoutWarehouse_Allowed()
    {
        var product = await Product();

        var errors = await _service.Validate(Document(InventoryDocumentType.Receipt, Line(product, 2)));

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public async Task Validate_BundleProduct_Error()
    {
        var product = await Product(p => p.ManageInventoryMethodId = ManageInventoryMethod.ManageStockByBundleProducts);

        var errors = await _service.Validate(Document(InventoryDocumentType.Receipt, Line(product, 2)));

        CollectionAssert.Contains(errors.ToList(), StockErrors.StockNotTracked);
    }

    [TestMethod]
    public async Task Validate_NoLines_Error()
    {
        var errors = await _service.Validate(Document(InventoryDocumentType.Receipt));

        CollectionAssert.Contains(errors.ToList(), InventoryDocumentErrors.NoLines);
    }

    [TestMethod]
    public async Task UpdateDraft_Posted_Rejected()
    {
        var product = await Product();
        var document = Document(InventoryDocumentType.Receipt, Line(product, 2));
        document.StatusId = InventoryDocumentStatus.Posted;
        await _documents.InsertAsync(document);

        document.Comment = "changed";
        var errors = await _service.UpdateDraft(document);

        CollectionAssert.AreEqual(new[] { InventoryDocumentErrors.Posted }, errors.ToArray());
        Assert.AreEqual("comment", _documents.Table.Single().Comment);
    }

    [TestMethod]
    public async Task DeleteDraft_Posted_Rejected()
    {
        var document = Document(InventoryDocumentType.Receipt);
        document.StatusId = InventoryDocumentStatus.Posted;
        await _documents.InsertAsync(document);

        var errors = await _service.DeleteDraft(document.Id);

        CollectionAssert.AreEqual(new[] { InventoryDocumentErrors.Posted }, errors.ToArray());
        Assert.AreEqual(1, _documents.Table.Count());
    }

    [TestMethod]
    public async Task DeleteDraft_Draft_Deleted()
    {
        var document = await _service.CreateDraft(Document(InventoryDocumentType.Receipt));

        var errors = await _service.DeleteDraft(document.Id);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(0, _documents.Table.Count());
    }

    [TestMethod]
    public async Task UpdateDraft_LineAlreadyBooked_Rejected()
    {
        var product = await Product();
        var line = Line(product, 2);
        var document = await _service.CreateDraft(Document(InventoryDocumentType.Receipt, line));
        //a posting that stopped after booking the line
        await _journal.InsertAsync(new InventoryJournal {
            ProductId = product.Id, ObjectType = nameof(InventoryDocument), ObjectId = document.Id,
            PositionId = line.Id, InQty = 2, MovementTypeId = InventoryMovementType.Receipt
        });

        document.Lines.Single().Quantity = 5;
        var errors = await _service.UpdateDraft(document);

        CollectionAssert.AreEqual(new[] { InventoryDocumentErrors.LineBooked }, errors.ToArray());
        Assert.AreEqual(2, _documents.Table.Single().Lines.Single().Quantity);
    }

    [TestMethod]
    public async Task UpdateDraft_StockCountWarehouse_Kept()
    {
        var product = await Product();
        var document = await _service.CreateDraft(Document(InventoryDocumentType.StockCount,
            new InventoryDocumentLine { ProductId = product.Id, ExpectedQuantity = 10 }));

        document.WarehouseId = "w2";
        await _service.UpdateDraft(document);

        Assert.AreEqual("w1", _documents.Table.Single().WarehouseId);
    }

    [TestMethod]
    public async Task UpdateDraft_Draft_Saved()
    {
        var product = await Product();
        var document = await _service.CreateDraft(Document(InventoryDocumentType.Receipt, Line(product, 2)));

        document.Lines.Single().Quantity = 5;
        document.SupplierName = "Supplier";
        var errors = await _service.UpdateDraft(document);

        Assert.AreEqual(0, errors.Count);
        var stored = _documents.Table.Single();
        Assert.AreEqual(5, stored.Lines.Single().Quantity);
        Assert.AreEqual("Supplier", stored.SupplierName);
        Assert.AreEqual(document.Number, stored.Number);
    }

    [TestMethod]
    public async Task Search_FiltersByStoreAndType()
    {
        await _service.CreateDraft(Document(InventoryDocumentType.Receipt));
        var shared = Document(InventoryDocumentType.Receipt);
        shared.WarehouseId = "w2";
        await _service.CreateDraft(shared);
        await _service.CreateDraft(Document(InventoryDocumentType.Adjustment));

        var found = await _service.Search(new InventoryDocumentSearch
            { StoreId = "store1", Type = InventoryDocumentType.Receipt });

        Assert.AreEqual(1, found.Count);
        Assert.AreEqual("w1", found.Single().WarehouseId);
    }
}
