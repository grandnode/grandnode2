using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
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
        _service = new InventoryDocumentService(_documents, _products, _journal, StockTestFixture.Warehouses(),
            new MongoResourceLockTest(), settingService.Object, new Mock<IMediator>().Object);
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
