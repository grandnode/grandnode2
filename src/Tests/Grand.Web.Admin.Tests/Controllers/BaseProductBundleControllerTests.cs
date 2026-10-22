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
public class BaseProductBundleControllerTests : ProductControllerTestsBase<BaseProductBundleControllerTests.TestProductBundleController>
{
    // BaseProductBundleController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductBundleController(
        IProductAddPopupViewModelService productAddPopupViewModelService,
        IProductBundleViewModelService productBundleViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductBundleController(productAddPopupViewModelService, productBundleViewModelService, productService, translationService, scope);

    protected override TestProductBundleController CreateController() => new(
        _productAddPopupViewModelServiceMock.Object,
        _productBundleViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- BundleProductList ------------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList/SimilarProductList above.

    [TestMethod]
    public async Task BundleProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.BundleProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task BundleProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.BundleProducts.Add(new BundleProduct { Id = "r1", ProductId = "p2", DisplayOrder = 0, Quantity = 3 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.BundleProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- BundleProductUpdate ----------------------------------------------------------------------

    [TestMethod]
    public async Task BundleProductUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.BundleProductModel { ProductBundleId = "p1" };

        var result = await _controller.BundleProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productBundleViewModelServiceMock.Verify(s => s.UpdateBundleProductModel(It.IsAny<ProductModel.BundleProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task BundleProductUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.BundleProductModel { ProductBundleId = "p1" };

        var result = await _controller.BundleProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productBundleViewModelServiceMock.Verify(s => s.UpdateBundleProductModel(model), Times.Once);
    }

    // --- BundleProductDelete ----------------------------------------------------------------------

    [TestMethod]
    public async Task BundleProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.BundleProductModel { ProductBundleId = "p1" };

        var result = await _controller.BundleProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productBundleViewModelServiceMock.Verify(s => s.DeleteBundleProductModel(It.IsAny<ProductModel.BundleProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task BundleProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.BundleProductModel { ProductBundleId = "p1" };

        var result = await _controller.BundleProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productBundleViewModelServiceMock.Verify(s => s.DeleteBundleProductModel(model), Times.Once);
    }

    // --- BundleProductAddPopup (GET) --------------------------------------------------------------

    [TestMethod]
    public async Task BundleProductAddPopupGet_CallsPrepareBundleProductModel()
    {
        // PrepareBundleProductModel() now reads scope internally (Task 9).
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareBundleProductModel())
            .ReturnsAsync(new ProductModel.AddBundleProductModel());

        var result = await _controller.BundleProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddBundleProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productAddPopupViewModelServiceMock.Verify(s => s.PrepareBundleProductModel(), Times.Once);
    }

    // --- BundleProductAddPopupList -----------------------------------------------------------------

    [TestMethod]
    public async Task BundleProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddBundleProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddBundleProductModel();
        var result = await _controller.BundleProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task BundleProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddBundleProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddBundleProductModel { SearchStoreId = "explicit" };
        var result = await _controller.BundleProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- BundleProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original BundleProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach bundle-product mappings onto another vendor's
    // product by posting its id.

    [TestMethod]
    public async Task BundleProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddBundleProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.BundleProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productBundleViewModelServiceMock.Verify(s => s.InsertBundleProductModel(It.IsAny<ProductModel.AddBundleProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task BundleProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddBundleProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.BundleProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productBundleViewModelServiceMock.Verify(s => s.InsertBundleProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task BundleProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reprepared = new ProductModel.AddBundleProductModel();
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareBundleProductModel()).ReturnsAsync(reprepared);
        var model = new ProductModel.AddBundleProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.BundleProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productBundleViewModelServiceMock.Verify(s => s.InsertBundleProductModel(It.IsAny<ProductModel.AddBundleProductModel>()), Times.Never);
    }
}
