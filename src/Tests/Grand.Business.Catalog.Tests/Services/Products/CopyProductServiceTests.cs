using Grand.Business.Catalog.Services.Products;
using Grand.Business.Catalog.Tests.Services.Inventory;
using Grand.Business.Common.Services.Seo;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class CopyProductServiceTests
{
    private CopyProductService _copyProductService;
    private Mock<ILanguageService> _langServiceMock;
    private Mock<IProductService> _productServiceMock;
    private SeoSettings _settings;
    private Mock<ISlugService> _slugServiceMock;
    private ISeNameService _seNameService;
    
    public CopyProductServiceTests()
    {
        _productServiceMock = new Mock<IProductService>();
        _langServiceMock = new Mock<ILanguageService>();
        _slugServiceMock = new Mock<ISlugService>();
        _settings = new SeoSettings();
        _seNameService = new SeNameService(_slugServiceMock.Object, _langServiceMock.Object, new SeoSettings());
        _copyProductService = new CopyProductService(_productServiceMock.Object, _slugServiceMock.Object, _seNameService);
    }

    [TestMethod]
    public async Task CopyProduct_CopyHasNoStock_KeepsWarehouses()
    {
        _langServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        var product = new Product {
            Name = "name", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 6,
            ReservedQuantity = 2, WarehouseId = "w1", UseMultipleWarehouses = true,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = 6, ReservedQuantity = 2 }
            }
        };

        var copy = await _copyProductService.CopyProduct(product, "copy-product");

        Assert.AreEqual(0, copy.StockQuantity);
        Assert.AreEqual(0, copy.ReservedQuantity);
        Assert.IsTrue(copy.LowStock);
        Assert.AreEqual("w1", copy.WarehouseId);
        Assert.IsTrue(copy.UseMultipleWarehouses);
        var row = copy.ProductWarehouseInventory.Single();
        Assert.AreEqual("w2", row.WarehouseId);
        Assert.AreEqual(0, row.StockQuantity);
        Assert.AreEqual(0, row.ReservedQuantity);
        //the source keeps its stock
        Assert.AreEqual(6, product.StockQuantity);
        Assert.AreEqual(6, product.ProductWarehouseInventory.Single().StockQuantity);
    }


    [TestMethod]
    public async Task CopyProduct_Tracked_CopyHasNoStock_KeepsTrackingSettings()
    {
        _langServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        var combination = new ProductAttributeCombination {
            Attributes = StockTestFixture.Attributes("v1"), StockQuantity = 3, Sku = "s1",
            WarehouseInventory = { new ProductCombinationWarehouseInventory { WarehouseId = "w1", StockQuantity = 3 } },
            TierPrices = { new ProductCombinationTierPrices { Quantity = 5, Price = 9 } }
        };
        var product = new Product {
            Name = "name", ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            UseMultipleWarehouses = true, StockQuantity = 3, InventoryTrackingModeId = InventoryTrackingMode.SerialNumber,
            WarrantyMonths = 12, ExpiryWarningDays = 5, ProductAttributeCombinations = { combination },
            ProductWarehouseInventory = { new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 3 } }
        };

        var copy = await _copyProductService.CopyProduct(product, "copy-product");

        Assert.AreEqual(0, copy.StockQuantity);
        Assert.AreEqual(0, copy.ProductWarehouseInventory.Single().StockQuantity);
        Assert.AreEqual("w1", copy.ProductWarehouseInventory.Single().WarehouseId);
        var copied = copy.ProductAttributeCombinations.Single();
        Assert.AreEqual(0, copied.StockQuantity);
        Assert.AreEqual(0, copied.WarehouseInventory.Single().StockQuantity);
        Assert.AreEqual("s1", copied.Sku);
        Assert.AreEqual(5, copied.TierPrices.Single().Quantity);
        Assert.AreEqual(InventoryTrackingMode.SerialNumber, copy.InventoryTrackingModeId);
        Assert.AreEqual(12, copy.WarrantyMonths);
        Assert.AreEqual(5, copy.ExpiryWarningDays);
        //the source keeps its stock
        Assert.AreEqual(3, combination.StockQuantity);
        Assert.AreEqual(3, product.ProductWarehouseInventory.Single().StockQuantity);
    }

    [TestMethod]
    public async Task CopyProduct_InserAndReturnNewProduct()
    {
        _langServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        var product = new Product {
            Name = "name",
            Price = 49,
            ShortDescription = "Desc"
        };
        var copy = await _copyProductService.CopyProduct(product, "copy-product");
        Assert.AreEqual(copy.Price, product.Price);
        Assert.AreEqual(copy.ShortDescription, product.ShortDescription);


        Assert.AreNotEqual(copy.SeName, product.SeName);
        Assert.AreNotEqual(copy.Id, product.Id);
        Assert.AreNotEqual(copy.Name, product.Name);
        _productServiceMock.Verify(c => c.InsertProduct(It.IsAny<Product>()), Times.Once);
    }
}