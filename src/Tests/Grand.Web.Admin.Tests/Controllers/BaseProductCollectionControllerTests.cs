using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseProductCollectionControllerTests : ProductControllerTestsBase<BaseProductCollectionControllerTests.TestProductCollectionController>
{
    // BaseProductCollectionController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductCollectionController(
        IProductCollectionViewModelService productCollectionViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductCollectionController(productCollectionViewModelService, productService, translationService, scope);

    protected override TestProductCollectionController CreateController() => new(
        _productCollectionViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- ProductCollectionList ------------------------------------------------------------------
    // Same shape as ProductCategoryList above: HasAccess (strict) mirrors Store's CanAccessProduct and
    // Vendor's CheckAccessToProduct gating this action on both hosts.

    [TestMethod]
    public async Task ProductCollectionList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductCollectionList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCollectionViewModelServiceMock.Verify(s => s.PrepareProductCollectionModel(It.IsAny<Product>()), Times.Never);
    }

    // Guards against the same regression class found in "Product categories": the denial message must be
    // templated via scope.ResourceKeyPrefix, not hardcoded to "Admin." - "Vendor.Catalog.Products.Permissions"
    // exists at the XML resource layer (en_220.xml) even though it has no Vendor call site in this region.
    [TestMethod]
    public async Task ProductCollectionList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductCollectionList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductCollectionList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _productCollectionViewModelServiceMock.Setup(s => s.PrepareProductCollectionModel(product))
            .ReturnsAsync(new List<ProductModel.ProductCollectionModel> { new() { Id = "c1", ProductId = "p1" } });

        var result = await _controller.ProductCollectionList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductCollectionInsert ----------------------------------------------------------------

    [TestMethod]
    public async Task ProductCollectionInsert_ScopeDeniesAccess_ReturnsErrorJson_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductCollectionModel { ProductId = "p1" };

        var result = await _controller.ProductCollectionInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCollectionViewModelServiceMock.Verify(s => s.InsertProductCollection(It.IsAny<ProductModel.ProductCollectionModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCollectionInsert_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductCollectionModel { ProductId = "p1" };

        var result = await _controller.ProductCollectionInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productCollectionViewModelServiceMock.Verify(s => s.InsertProductCollection(model), Times.Once);
    }

    // --- ProductCollectionUpdate ----------------------------------------------------------------

    [TestMethod]
    public async Task ProductCollectionUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductCollectionModel { ProductId = "p1" };

        var result = await _controller.ProductCollectionUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCollectionViewModelServiceMock.Verify(s => s.UpdateProductCollection(It.IsAny<ProductModel.ProductCollectionModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCollectionUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductCollectionModel { ProductId = "p1" };

        var result = await _controller.ProductCollectionUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productCollectionViewModelServiceMock.Verify(s => s.UpdateProductCollection(model), Times.Once);
    }

    // --- ProductCollectionDelete ----------------------------------------------------------------

    [TestMethod]
    public async Task ProductCollectionDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductCollectionModel { Id = "c1", ProductId = "p1" };

        var result = await _controller.ProductCollectionDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCollectionViewModelServiceMock.Verify(s => s.DeleteProductCollection(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCollectionDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductCollectionModel { Id = "c1", ProductId = "p1" };

        var result = await _controller.ProductCollectionDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productCollectionViewModelServiceMock.Verify(s => s.DeleteProductCollection("c1", "p1"), Times.Once);
    }
}
