using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ProductBundleViewModelServiceTests
{
    private Mock<IProductService> _productServiceMock;
    private ProductBundleViewModelService _productBundleViewModelService;
    private Mock<IAdminDataScope<Product>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _productBundleViewModelService = new ProductBundleViewModelService(_productServiceMock.Object, _scopeMock.Object);
    }

    private void SetupOwnershipForInsertTests(Product ownedProduct, Product notOwnedProduct)
    {
        _productServiceMock.Setup(p => p.GetProductById(ownedProduct.Id, It.IsAny<bool>())).ReturnsAsync(ownedProduct);
        _productServiceMock.Setup(p => p.GetProductById(notOwnedProduct.Id, It.IsAny<bool>())).ReturnsAsync(notOwnedProduct);
        _scopeMock.Setup(s => s.HasAccess(ownedProduct)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(notOwnedProduct)).ReturnsAsync(false);
    }

    [TestMethod]
    public async Task InsertBundleProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddBundleProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productBundleViewModelService.InsertBundleProductModel(model);

        Assert.IsTrue(mainProduct.BundleProducts.Any(x => x.ProductId == "owned"),
            "The owned selected product should be inserted.");
        Assert.IsFalse(mainProduct.BundleProducts.Any(x => x.ProductId == "not-owned"),
            "A not-owned selected product must be silently dropped, not inserted.");
        _productServiceMock.Verify(p => p.InsertBundleProduct(It.Is<BundleProduct>(r => r.ProductId == "not-owned"), It.IsAny<string>()), Times.Never);
    }
}
