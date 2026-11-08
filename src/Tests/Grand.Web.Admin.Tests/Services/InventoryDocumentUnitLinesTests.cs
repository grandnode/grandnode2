using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class InventoryDocumentUnitLinesTests
{
    private readonly Mock<IProductService> _productService = new();
    private InventoryDocumentViewModelService _service;

    [TestInitialize]
    public void Init()
    {
        var productScope = new Mock<IAdminDataScope<Product>>();
        productScope.Setup(x => x.HasAccess(It.IsAny<Product>())).ReturnsAsync(true);
        var warehouseService = new Mock<IWarehouseService>();
        warehouseService.Setup(x => x.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse>(new List<Warehouse>(), 0, 1));
        _service = new InventoryDocumentViewModelService(new Mock<IInventoryDocumentService>().Object,
            warehouseService.Object, _productService.Object, new Mock<ICategoryService>().Object,
            new Mock<IBrandService>().Object, new Mock<IRequestTranslationService>().Object,
            new Mock<IEnumTranslationService>().Object, new Mock<IDateTimeService>().Object,
            new Mock<IAdminDataScope<Warehouse>>().Object, productScope.Object);
    }

    private Product Product(InventoryTrackingMode mode)
    {
        var product = new Product {
            Name = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, InventoryTrackingModeId = mode
        };
        _productService.Setup(x => x.GetProductById(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        return product;
    }

    private static (InventoryDocument, InventoryDocumentModel) Document(InventoryDocumentType type,
        InventoryDocumentLine line, InventoryDocumentLineModel lineModel)
    {
        lineModel.Id = line.Id;
        var document = new InventoryDocument { DocumentTypeId = type, WarehouseId = "w1", Lines = { line } };
        var model = new InventoryDocumentModel {
            DocumentTypeId = (int)type, WarehouseId = "w1", Lines = { lineModel }
        };
        return (document, model);
    }

    [TestMethod]
    public async Task PrepareLines_SerialLine_JoinsNumbersOnePerLine()
    {
        var product = Product(InventoryTrackingMode.SerialNumber);
        var document = new InventoryDocument {
            DocumentTypeId = InventoryDocumentType.Receipt,
            Lines = { new InventoryDocumentLine { ProductId = product.Id, Quantity = 2, SerialNumbers = ["A1", "A2"] } }
        };
        var model = new InventoryDocumentModel();

        await _service.PrepareModel(model, document);

        var line = model.Lines.Single();
        Assert.AreEqual("A1\nA2", line.SerialNumbersText);
        Assert.AreEqual(InventoryTrackingMode.SerialNumber, line.TrackingMode);
    }

    [TestMethod]
    public async Task MapLines_SerialNumbersText_ParsedTrimmedInOrder_QuantityFollowsCount()
    {
        var product = Product(InventoryTrackingMode.SerialNumber);
        var (document, model) = Document(InventoryDocumentType.Receipt,
            new InventoryDocumentLine { ProductId = product.Id },
            new InventoryDocumentLineModel { Quantity = 1, SerialNumbersText = " B2\r\nA1 \n\n" });

        await _service.ApplyModel(document, model);

        var line = document.Lines.Single();
        CollectionAssert.AreEqual(new[] { "B2", "A1" }, line.SerialNumbers.ToArray());
        Assert.AreEqual(2, line.Quantity);
    }

    [TestMethod]
    public async Task MapLines_SerialAdjustmentOut_QuantityKeepsItsSign()
    {
        var product = Product(InventoryTrackingMode.SerialNumber);
        var (document, model) = Document(InventoryDocumentType.Adjustment,
            new InventoryDocumentLine { ProductId = product.Id },
            new InventoryDocumentLineModel { Quantity = -1, SerialNumbersText = "A1\nA2" });

        await _service.ApplyModel(document, model);

        Assert.AreEqual(-2, document.Lines.Single().Quantity);
    }

    [TestMethod]
    public async Task MapLines_SerialStockCount_CountedIsScannedNumbers()
    {
        var product = Product(InventoryTrackingMode.SerialNumber);
        var (document, model) = Document(InventoryDocumentType.StockCount,
            new InventoryDocumentLine { ProductId = product.Id, ExpectedQuantity = 3 },
            new InventoryDocumentLineModel { CountedQuantity = 7, SerialNumbersText = "A1\nA2" });

        await _service.ApplyModel(document, model);

        Assert.AreEqual(2, document.Lines.Single().CountedQuantity);
    }

    [TestMethod]
    public async Task MapLines_BatchAndExpiryDate_StoredAsUtcDate()
    {
        var product = Product(InventoryTrackingMode.Batch);
        var (document, model) = Document(InventoryDocumentType.Receipt,
            new InventoryDocumentLine { ProductId = product.Id },
            new InventoryDocumentLineModel {
                Quantity = 4, BatchNumber = " L1 ", ExpiryDate = new DateTime(2027, 1, 31, 15, 30, 0)
            });

        await _service.ApplyModel(document, model);

        var line = document.Lines.Single();
        Assert.AreEqual("L1", line.BatchNumber);
        Assert.AreEqual(new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc), line.ExpiryDateUtc);
        Assert.AreEqual(DateTimeKind.Utc, line.ExpiryDateUtc!.Value.Kind);
    }

    [TestMethod]
    public async Task MapLines_UntrackedProduct_UnitsIgnored()
    {
        var product = Product(InventoryTrackingMode.None);
        var (document, model) = Document(InventoryDocumentType.Receipt,
            new InventoryDocumentLine { ProductId = product.Id },
            new InventoryDocumentLineModel { Quantity = 4, BatchNumber = "L1", SerialNumbersText = "A1" });

        await _service.ApplyModel(document, model);

        var line = document.Lines.Single();
        Assert.IsNull(line.BatchNumber);
        Assert.AreEqual(0, line.SerialNumbers.Count);
        Assert.AreEqual(4, line.Quantity);
    }
}
