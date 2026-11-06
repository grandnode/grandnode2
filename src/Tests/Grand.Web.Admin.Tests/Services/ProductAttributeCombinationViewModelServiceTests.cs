using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Customers;
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
    private Mock<IStockMovementService> _stockMovementServiceMock;
    private Mock<ISettingService> _settingServiceMock;
    private Mock<IProductAttributeService> _productAttributeServiceMock;
    private Mock<IWarehouseService> _warehouseServiceMock;
    private ProductAttributeCombinationViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _stockMovementServiceMock = new Mock<IStockMovementService>();
        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock.Setup(x => x.LoadSetting<InventorySettings>(It.IsAny<string>()))
            .ReturnsAsync(new InventorySettings());
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "staff" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        _productAttributeServiceMock = new Mock<IProductAttributeService>();
        _warehouseServiceMock = new Mock<IWarehouseService>();

        _service = new ProductAttributeCombinationViewModelService(
            new Mock<IPictureService>().Object,
            _productAttributeServiceMock.Object,
            new Mock<ICurrencyService>().Object,
            new Mock<IRequestTranslationService>().Object,
            contextAccessor.Object,
            new Mock<IGroupService>().Object,
            _warehouseServiceMock.Object,
            new Mock<IStoreService>().Object,
            new Mock<IProductAttributeFormatter>().Object,
            new CurrencySettings(),
            _stockMovementServiceMock.Object,
            _settingServiceMock.Object);
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

    private static bool Change(IReadOnlyList<StockChange> changes, string warehouseId, int quantity, int? reserved) =>
        changes.Count == 1 && changes[0].WarehouseId == warehouseId && changes[0].SetTo &&
        changes[0].Quantity == quantity && changes[0].ReservedQuantity == reserved &&
        changes[0].MovementType == InventoryMovementType.ManualEdit;

    [TestMethod]
    public async Task Insert_ManageStockByAttributes_AppliesTheCombinationStockThroughTheLedger()
    {
        var product = ProductWithOneCombination();
        var model = new ProductAttributeCombinationModel {
            SelectedAttributes = [new CustomAttributeModel { Key = "m1", Value = "v1" }],
            StockQuantity = 7,
            ReservedQuantity = 2
        };

        var warnings = await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        Assert.AreEqual(0, warnings.Count);
        _productAttributeServiceMock.Verify(s => s.InsertProductAttributeCombination(
            It.IsAny<ProductAttributeCombination>(), "p1"), Times.Once);
        _stockMovementServiceMock.Verify(s => s.Apply("p1",
            It.Is<IReadOnlyList<StockChange>>(c => Change(c, "", 7, 2) && c[0].Attributes[0].Value == "v1")),
            Times.Once);
    }

    [TestMethod]
    public async Task CombinationPopup_Update_StockChanged_AppliesSetToForCombination()
    {
        var product = ProductWithOneCombination();
        var model = new ProductAttributeCombinationModel {
            Id = "c1",
            ProductId = "another-product",
            StockQuantity = 3,
            OriginalStockQuantity = 5,
            ReservedQuantity = 1,
            OriginalReservedQuantity = 1
        };

        var warnings = await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        Assert.AreEqual(0, warnings.Count);
        _productAttributeServiceMock.Verify(s => s.UpdateProductAttributeCombination(
            It.Is<ProductAttributeCombination>(c => c.Id == "c1"), "p1"), Times.Once);
        _stockMovementServiceMock.Verify(s => s.Apply("p1",
            It.Is<IReadOnlyList<StockChange>>(c => Change(c, "", 3, null))), Times.Once);
        _stockMovementServiceMock.Verify(s => s.Apply("another-product", It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CombinationPopup_Update_StockUntouched_AppliesNothing()
    {
        var product = ProductWithOneCombination();
        var model = new ProductAttributeCombinationModel {
            Id = "c1", StockQuantity = 5, OriginalStockQuantity = 5, ReservedQuantity = 1,
            OriginalReservedQuantity = 1, Sku = "sku"
        };

        await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        _stockMovementServiceMock.Verify(s => s.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
        _productAttributeServiceMock.Verify(s => s.UpdateProductAttributeCombination(
            It.Is<ProductAttributeCombination>(c => c.Sku == "sku"), "p1"), Times.Once);
    }

    [TestMethod]
    public async Task CombinationPopup_SettingOn_StockChanged_RefusedWithoutSaving()
    {
        _settingServiceMock.Setup(x => x.LoadSetting<InventorySettings>(It.IsAny<string>()))
            .ReturnsAsync(new InventorySettings { StockChangesOnlyThroughDocuments = true });
        var product = ProductWithOneCombination();
        var model = new ProductAttributeCombinationModel
            { Id = "c1", StockQuantity = 9, OriginalStockQuantity = 5, Sku = "sku" };

        var warnings = await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        Assert.AreEqual(1, warnings.Count);
        _productAttributeServiceMock.Verify(s => s.UpdateProductAttributeCombination(
            It.IsAny<ProductAttributeCombination>(), It.IsAny<string>()), Times.Never);
        _stockMovementServiceMock.Verify(s => s.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
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
        _stockMovementServiceMock.Verify(s => s.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Update_MultipleWarehouses_AppliesTheChangedRowAndMarksItUsed()
    {
        var product = ProductWithOneCombination();
        product.UseMultipleWarehouses = true;
        var model = new ProductAttributeCombinationModel {
            Id = "c1",
            WarehouseInventoryModels = [
                new ProductAttributeCombinationModel.WarehouseInventoryModel {
                    WarehouseId = "w1", WarehouseUsed = true, StockQuantity = 4, ReservedQuantity = 2
                }
            ]
        };

        await _service.InsertOrUpdateProductAttributeCombinationPopup(product, model);

        _stockMovementServiceMock.Verify(s => s.Apply("p1",
            It.Is<IReadOnlyList<StockChange>>(c => Change(c, "w1", 4, 2))), Times.Once);
        _productAttributeServiceMock.Verify(s => s.UpdateProductAttributeCombination(
            It.Is<ProductAttributeCombination>(c => c.WarehouseInventory.Single().WarehouseId == "w1"), "p1"),
            Times.Once);
    }
}
