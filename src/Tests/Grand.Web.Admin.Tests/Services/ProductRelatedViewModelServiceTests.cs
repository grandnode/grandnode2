using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ProductRelatedViewModelServiceTests
{
    private Mock<IProductService> _productServiceMock;
    private ProductRelatedViewModelService _productRelatedViewModelService;
    private Mock<IAdminDataScope<Product>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _productRelatedViewModelService = new ProductRelatedViewModelService(_productServiceMock.Object, _scopeMock.Object);
    }

    private void SetupOwnershipForInsertTests(Product ownedProduct, Product notOwnedProduct)
    {
        _productServiceMock.Setup(p => p.GetProductById(ownedProduct.Id, It.IsAny<bool>())).ReturnsAsync(ownedProduct);
        _productServiceMock.Setup(p => p.GetProductById(notOwnedProduct.Id, It.IsAny<bool>())).ReturnsAsync(notOwnedProduct);
        _scopeMock.Setup(s => s.HasAccess(ownedProduct)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(notOwnedProduct)).ReturnsAsync(false);
    }

    [TestMethod]
    public async Task InsertRelatedProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddRelatedProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productRelatedViewModelService.InsertRelatedProductModel(model);

        Assert.IsTrue(mainProduct.RelatedProducts.Any(x => x.ProductId2 == "owned"),
            "The owned selected product should be inserted.");
        Assert.IsFalse(mainProduct.RelatedProducts.Any(x => x.ProductId2 == "not-owned"),
            "A not-owned selected product must be silently dropped, not inserted.");
        _productServiceMock.Verify(p => p.InsertRelatedProduct(It.Is<RelatedProduct>(r => r.ProductId2 == "not-owned"), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertSimilarProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddSimilarProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productRelatedViewModelService.InsertSimilarProductModel(model);

        Assert.IsTrue(mainProduct.SimilarProducts.Any(x => x.ProductId2 == "owned"),
            "The owned selected product should be inserted.");
        Assert.IsFalse(mainProduct.SimilarProducts.Any(x => x.ProductId2 == "not-owned"),
            "A not-owned selected product must be silently dropped, not inserted.");
        _productServiceMock.Verify(p => p.InsertSimilarProduct(It.Is<SimilarProduct>(r => r.ProductId2 == "not-owned")), Times.Never);
    }

    [TestMethod]
    public async Task InsertCrossSellProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddCrossSellProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productRelatedViewModelService.InsertCrossSellProductModel(model);

        _productServiceMock.Verify(p => p.InsertCrossSellProduct(It.Is<CrossSellProduct>(r => r.ProductId2 == "owned")), Times.Once);
        _productServiceMock.Verify(p => p.InsertCrossSellProduct(It.Is<CrossSellProduct>(r => r.ProductId2 == "not-owned")), Times.Never,
            "A not-owned selected product must be silently dropped, not inserted.");
    }

    [TestMethod]
    public async Task InsertRecommendedProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddRecommendedProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productRelatedViewModelService.InsertRecommendedProductModel(model);

        _productServiceMock.Verify(p => p.InsertRecommendedProduct("main", "owned"), Times.Once);
        _productServiceMock.Verify(p => p.InsertRecommendedProduct("main", "not-owned"), Times.Never,
            "A not-owned selected product must be silently dropped, not inserted.");
    }
}
