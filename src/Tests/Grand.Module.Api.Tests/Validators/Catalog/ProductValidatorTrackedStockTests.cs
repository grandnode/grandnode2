using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Validators.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Validators.Catalog;

[TestClass]
public class ProductValidatorTrackedStockTests
{
    private readonly Mock<IProductService> _productService = new();
    private ProductValidator _validator;

    [TestInitialize]
    public void Init()
    {
        var translation = new Mock<ITranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>())).Returns("message");
        _validator = new ProductValidator([], translation.Object,
            new Mock<IContextAccessor>().Object, _productService.Object, new Mock<IProductLayoutService>().Object,
            new Mock<IBrandService>().Object, new Mock<IVendorService>().Object, new CommonSettings());
    }

    private void Stored(InventoryTrackingMode mode, int stock)
    {
        _productService.Setup(x => x.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, InventoryTrackingModeId = mode,
            StockQuantity = stock
        });
    }

    private async Task<bool> RefusedForStock(ProductDto dto) =>
        (await _validator.ValidateAsync(dto)).Errors.Any(x =>
            x.ErrorMessage == InventoryTracking.StockDocumentsOnlyMessage);

    [TestMethod]
    public async Task TrackedProduct_StockChanged_Refused()
    {
        Stored(InventoryTrackingMode.Batch, 5);

        Assert.IsTrue(await RefusedForStock(new ProductDto { Id = "p1", Name = "p", StockQuantity = 9 }));
    }

    [TestMethod]
    public async Task TrackedProduct_StockUnchanged_Allowed()
    {
        Stored(InventoryTrackingMode.Batch, 5);

        Assert.IsFalse(await RefusedForStock(new ProductDto { Id = "p1", Name = "p", StockQuantity = 5 }));
    }

    [TestMethod]
    public async Task UntrackedProduct_StockChanged_Allowed()
    {
        Stored(InventoryTrackingMode.None, 5);

        Assert.IsFalse(await RefusedForStock(new ProductDto { Id = "p1", Name = "p", StockQuantity = 9 }));
    }
}
