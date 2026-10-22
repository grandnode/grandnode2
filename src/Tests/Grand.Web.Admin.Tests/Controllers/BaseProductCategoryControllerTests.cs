using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain;
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
public class BaseProductCategoryControllerTests : ProductControllerTestsBase<BaseProductCategoryControllerTests.TestProductCategoryController>
{
    // BaseProductCategoryController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductCategoryController(
        IProductCategoryViewModelService productCategoryViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductCategoryController(productCategoryViewModelService, productService, translationService, scope);

    protected override TestProductCategoryController CreateController() => new(
        _productCategoryViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- ProductCategoryList ----------------------------------------------------------------------
    // HasAccess (strict), not CanView: mirrors Store's CanAccessProduct (AccessToEntityByStore) and
    // Vendor's CheckAccessToProduct (VendorId equality) gating this action on both hosts.

    [TestMethod]
    public async Task ProductCategoryList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductCategoryList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCategoryViewModelServiceMock.Verify(s => s.PrepareProductCategoryModel(It.IsAny<Product>()), Times.Never);
    }

    // Guards against a real regression found in review: the denial message must be templated via
    // scope.ResourceKeyPrefix, not hardcoded to "Admin." - "Vendor.Catalog.Products.Permissions" exists
    // at the XML resource layer (en_220.xml) even though Task 6's narrower file-scoped audit never saw a
    // Vendor call site for it.
    [TestMethod]
    public async Task ProductCategoryList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductCategoryList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductCategoryList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _productCategoryViewModelServiceMock.Setup(s => s.PrepareProductCategoryModel(product))
            .ReturnsAsync(new List<ProductModel.ProductCategoryModel> { new() { Id = "c1", ProductId = "p1" } });

        var result = await _controller.ProductCategoryList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // The tabs of a product the Store staff member can see but not edit (shared with other stores)
    // must load: reads are gated by CanView, only the mutations by HasAccess.
    [TestMethod]
    public async Task ProductCategoryList_ViewableButNotOwned_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        _productCategoryViewModelServiceMock.Setup(s => s.PrepareProductCategoryModel(product))
            .ReturnsAsync(new List<ProductModel.ProductCategoryModel> { new() { Id = "c1", ProductId = "p1" } });

        var result = await _controller.ProductCategoryList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var gridModel = (result as JsonResult)?.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    [TestMethod]
    public async Task ProductCategoryInsert_ViewableButNotOwned_IsDenied()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductCategoryInsert(new ProductModel.ProductCategoryModel { ProductId = "p1" });

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCategoryViewModelServiceMock.Verify(
            s => s.InsertProductCategoryModel(It.IsAny<ProductModel.ProductCategoryModel>()), Times.Never);
    }

    // --- ProductCategoryInsert ---------------------------------------------------------------------

    [TestMethod]
    public async Task ProductCategoryInsert_ScopeDeniesAccess_ReturnsErrorJson_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductCategoryModel { ProductId = "p1" };

        var result = await _controller.ProductCategoryInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCategoryViewModelServiceMock.Verify(s => s.InsertProductCategoryModel(It.IsAny<ProductModel.ProductCategoryModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCategoryInsert_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductCategoryModel { ProductId = "p1" };

        var result = await _controller.ProductCategoryInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productCategoryViewModelServiceMock.Verify(s => s.InsertProductCategoryModel(model), Times.Once);
    }

    // --- ProductCategoryUpdate ---------------------------------------------------------------------

    [TestMethod]
    public async Task ProductCategoryUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductCategoryModel { ProductId = "p1" };

        var result = await _controller.ProductCategoryUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCategoryViewModelServiceMock.Verify(s => s.UpdateProductCategoryModel(It.IsAny<ProductModel.ProductCategoryModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCategoryUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductCategoryModel { ProductId = "p1" };

        var result = await _controller.ProductCategoryUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productCategoryViewModelServiceMock.Verify(s => s.UpdateProductCategoryModel(model), Times.Once);
    }

    // --- ProductCategoryDelete ---------------------------------------------------------------------

    [TestMethod]
    public async Task ProductCategoryDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductCategoryModel { Id = "c1", ProductId = "p1" };

        var result = await _controller.ProductCategoryDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productCategoryViewModelServiceMock.Verify(s => s.DeleteProductCategory(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCategoryDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductCategoryModel { Id = "c1", ProductId = "p1" };

        var result = await _controller.ProductCategoryDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productCategoryViewModelServiceMock.Verify(s => s.DeleteProductCategory("c1", "p1"), Times.Once);
    }
}
