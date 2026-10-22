using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ProductAttributeCombinationViewModelServiceTests
{
    private Mock<IInventoryManageService> _inventoryManageServiceMock;
    private Mock<IProductAttributeService> _productAttributeServiceMock;
    private Mock<IWarehouseService> _warehouseServiceMock;
    private ProductAttributeCombinationViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _inventoryManageServiceMock = new Mock<IInventoryManageService>();
        _productAttributeServiceMock = new Mock<IProductAttributeService>();
        _warehouseServiceMock = new Mock<IWarehouseService>();

        _service = new ProductAttributeCombinationViewModelService(
            _inventoryManageServiceMock.Object,
            new Mock<IPictureService>().Object,
            _productAttributeServiceMock.Object,
            new Mock<ICurrencyService>().Object,
            new Mock<IRequestTranslationService>().Object,
            new Mock<IContextAccessor>().Object,
            new Mock<IGroupService>().Object,
            _warehouseServiceMock.Object,
            new Mock<IStoreService>().Object,
            new Mock<IOutOfStockSubscriptionService>().Object,
            new Mock<IProductAttributeFormatter>().Object,
            new CurrencySettings());
    }

    private static Product ProductWithOneCombination()
    {
        var product = new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes
        };
        var mapping = new ProductAttributeMapping {
            Id = "m1",
            Combination = true,
            AttributeControlTypeId = AttributeControlType.DropdownList
        };
        mapping.ProductAttributeValues.Add(new ProductAttributeValue { Id = "v1" });
        mapping.ProductAttributeValues.Add(new ProductAttributeValue { Id = "v2" });
        product.ProductAttributeMappings.Add(mapping);
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination {
            Id = "c1",
            Attributes = [new CustomAttribute { Key = "m1", Value = "v2" }],
            StockQuantity = 5,
            ReservedQuantity = 1
        });
        return product;
    }

    [TestMethod]
    public async Task Insert_ManageStockByAttributes_ProductStockIncludesTheNewCombination()
    {
        var product = ProductWithOneCombination();
        var model = new ProductAttributeCombinationModel {
            SelectedAttributes = [new CustomAttributeModel { Key = "m1", Value = "v1" }],
            StockQuantity = 7,
            ReservedQuantity = 2
        };

        var warnings = await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        Assert.AreEqual(0, warnings.Count);
        _inventoryManageServiceMock.Verify(i => i.UpdateStockProduct(
            It.Is<Product>(p => p.Id == "p1" && p.StockQuantity == 12 && p.ReservedQuantity == 3), false), Times.Once);
    }

    [TestMethod]
    public async Task Update_ManageStockByAttributes_RecalculatesTheGivenProductNotModelProductId()
    {
        var product = ProductWithOneCombination();
        var model = new ProductAttributeCombinationModel {
            Id = "c1",
            ProductId = "another-product",
            StockQuantity = 3,
            ReservedQuantity = 0
        };

        var warnings = await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        Assert.AreEqual(0, warnings.Count);
        _productAttributeServiceMock.Verify(s => s.UpdateProductAttributeCombination(
            It.Is<ProductAttributeCombination>(c => c.Id == "c1"), "p1"), Times.Once);
        _inventoryManageServiceMock.Verify(i => i.UpdateStockProduct(
            It.Is<Product>(p => p.Id == "p1" && p.StockQuantity == 3), false), Times.Once);
        _inventoryManageServiceMock.Verify(i => i.UpdateStockProduct(
            It.Is<Product>(p => p.Id != "p1"), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Update_UnknownCombination_ReturnsWarningWithoutSaving()
    {
        var product = ProductWithOneCombination();
        var model = new ProductAttributeCombinationModel { Id = "missing" };

        var warnings = await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        Assert.AreEqual(1, warnings.Count);
        _productAttributeServiceMock.Verify(s => s.UpdateProductAttributeCombination(
            It.IsAny<ProductAttributeCombination>(), It.IsAny<string>()), Times.Never);
        _inventoryManageServiceMock.Verify(i => i.UpdateStockProduct(It.IsAny<Product>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Update_MultipleWarehouses_ReservedQuantityIsTheWarehouseSum()
    {
        var product = ProductWithOneCombination();
        product.UseMultipleWarehouses = true;
        _warehouseServiceMock.Setup(w => w.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse> { new() { Id = "w1" } });
        var model = new ProductAttributeCombinationModel {
            Id = "c1",
            StockQuantity = 100,
            ReservedQuantity = 90,
            WarehouseInventoryModels = [
                new ProductAttributeCombinationModel.WarehouseInventoryModel {
                    WarehouseId = "w1", WarehouseUsed = true, StockQuantity = 4, ReservedQuantity = 2
                }
            ]
        };

        await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        var combination = product.ProductAttributeCombinations.Single(c => c.Id == "c1");
        Assert.AreEqual(4, combination.StockQuantity);
        Assert.AreEqual(2, combination.ReservedQuantity);
        _inventoryManageServiceMock.Verify(i => i.UpdateStockProduct(
            It.Is<Product>(p => p.StockQuantity == 4 && p.ReservedQuantity == 2), false), Times.Once);
    }
}
