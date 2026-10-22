using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
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
public class BaseProductAssociatedControllerTests : ProductControllerTestsBase<BaseProductAssociatedControllerTests.TestProductAssociatedController>
{
    // BaseProductAssociatedController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductAssociatedController(
        IProductViewModelService productViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductAssociatedController(productViewModelService, productService, translationService, scope);

    protected override TestProductAssociatedController CreateController() => new(
        _productViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- AssociatedProductList -----------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList above. Admin's original had no
    // check at all.

    [TestMethod]
    public async Task AssociatedProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.AssociatedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task AssociatedProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _productServiceMock.Setup(p => p.GetAssociatedProducts("p1", CatalogAccess.IncludingHidden(), ""))
            .ReturnsAsync(new List<Product> { new() { Id = "a1", Name = "Assoc", DisplayOrder = 1 } });

        var result = await _controller.AssociatedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- AssociatedProductUpdate ---------------------------------------------------------------------

    [TestMethod]
    public async Task AssociatedProductUpdate_NotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.AssociatedProductModel { Id = "a1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.AssociatedProductUpdate(model));
    }

    [TestMethod]
    public async Task AssociatedProductUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var associatedProduct = new Product { Id = "a1" };
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync(associatedProduct);
        _scopeMock.Setup(s => s.HasAccess(associatedProduct)).ReturnsAsync(false);
        var model = new ProductModel.AssociatedProductModel { Id = "a1" };

        var result = await _controller.AssociatedProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(p => p.UpdateAssociatedProduct(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociatedProductUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var associatedProduct = new Product { Id = "a1" };
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync(associatedProduct);
        _scopeMock.Setup(s => s.HasAccess(associatedProduct)).ReturnsAsync(true);
        var model = new ProductModel.AssociatedProductModel { Id = "a1", DisplayOrder = 5 };

        var result = await _controller.AssociatedProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual(5, associatedProduct.DisplayOrder);
        _productServiceMock.Verify(p => p.UpdateAssociatedProduct(associatedProduct), Times.Once);
    }

    // --- AssociatedProductDelete ---------------------------------------------------------------------

    [TestMethod]
    public async Task AssociatedProductDelete_NotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.AssociatedProductModel { Id = "a1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.AssociatedProductDelete(model));
    }

    [TestMethod]
    public async Task AssociatedProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "a1" };
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AssociatedProductModel { Id = "a1" };

        var result = await _controller.AssociatedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.DeleteAssociatedProduct(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociatedProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "a1" };
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AssociatedProductModel { Id = "a1" };

        var result = await _controller.AssociatedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteAssociatedProduct(product), Times.Once);
    }

    // --- AssociatedProductAddPopup (GET) -------------------------------------------------------------

    [TestMethod]
    public async Task AssociatedProductAddPopupGet_CallsPrepareAssociatedProductModel()
    {
        // PrepareAssociatedProductModel() now reads scope internally (Task 9).
        _productViewModelServiceMock.Setup(s => s.PrepareAssociatedProductModel())
            .ReturnsAsync(new ProductModel.AddAssociatedProductModel());

        var result = await _controller.AssociatedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddAssociatedProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productViewModelServiceMock.Verify(s => s.PrepareAssociatedProductModel(), Times.Once);
    }

    // --- AssociatedProductAddPopupList ---------------------------------------------------------------

    [TestMethod]
    public async Task AssociatedProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddAssociatedProductModel();
        var result = await _controller.AssociatedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddAssociatedProductModel { SearchStoreId = "explicit" };
        var result = await _controller.AssociatedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- AssociatedProductAddPopup (POST) ------------------------------------------------------------
    // HasAccess (strict) on the parent product: closes a real gap - Vendor's controller had no ownership
    // check on the parent product (model.ProductId) at all, in either layer. Vendor's own (host-specific)
    // service already filtered each selected product via HasAccessToProduct before reparenting it
    // (Grand.Web.Vendor/Services/ProductViewModelService.cs InsertAssociatedProductModel), but the parent
    // was never checked anywhere, letting a vendor attach their own products under another vendor's
    // grouped product. This controller-level fix (parent HasAccess + per-selected-id HasAccess, matching
    // Store's original controller-level filtering) is necessary regardless of that pre-existing severity,
    // since BaseProductController uses AdminShared's unfiltered IProductViewModelService - once Vendor is
    // subclassed onto this base (Task 11), it loses its own service's per-id filter entirely, so the
    // controller must enforce both checks itself.

    [TestMethod]
    public async Task AssociatedProductAddPopupPost_ScopeDeniesAccessToParent_ReturnsContentMessage_DoesNotInsert()
    {
        var parent = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(parent);
        _scopeMock.Setup(s => s.HasAccess(parent)).ReturnsAsync(false);
        var model = new ProductModel.AddAssociatedProductModel { ProductId = "p1", SelectedProductIds = ["a1"] };

        var result = await _controller.AssociatedProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.InsertAssociatedProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopupPost_ScopeGrantsAccess_AllSelectedIdsAllowed_InsertsAll()
    {
        var parent = new Product { Id = "p1" };
        var selected = new Product { Id = "a1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(parent);
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync(selected);
        _scopeMock.Setup(s => s.HasAccess(parent)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(selected)).ReturnsAsync(true);
        var model = new ProductModel.AddAssociatedProductModel { ProductId = "p1", SelectedProductIds = ["a1"] };

        var result = await _controller.AssociatedProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        CollectionAssert.AreEqual(new[] { "a1" }, model.SelectedProductIds);
        _productViewModelServiceMock.Verify(s => s.InsertAssociatedProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopupPost_ScopeGrantsAccessToParent_FiltersOutDeniedSelectedIds()
    {
        var parent = new Product { Id = "p1" };
        var allowed = new Product { Id = "a1" };
        var denied = new Product { Id = "a2" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(parent);
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync(allowed);
        _productServiceMock.Setup(p => p.GetProductById("a2", false)).ReturnsAsync(denied);
        _scopeMock.Setup(s => s.HasAccess(parent)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(allowed)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(denied)).ReturnsAsync(false);
        var model = new ProductModel.AddAssociatedProductModel { ProductId = "p1", SelectedProductIds = ["a1", "a2"] };

        var result = await _controller.AssociatedProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        CollectionAssert.AreEqual(new[] { "a1" }, model.SelectedProductIds);
        _productViewModelServiceMock.Verify(s => s.InsertAssociatedProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopupPost_AllSelectedIdsDenied_DoesNotInsert()
    {
        var parent = new Product { Id = "p1" };
        var denied = new Product { Id = "a1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(parent);
        _productServiceMock.Setup(p => p.GetProductById("a1", false)).ReturnsAsync(denied);
        _scopeMock.Setup(s => s.HasAccess(parent)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(denied)).ReturnsAsync(false);
        var model = new ProductModel.AddAssociatedProductModel { ProductId = "p1", SelectedProductIds = ["a1"] };

        var result = await _controller.AssociatedProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.InsertAssociatedProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var parent = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(parent);
        _scopeMock.Setup(s => s.HasAccess(parent)).ReturnsAsync(true);
        var reprepared = new ProductModel.AddAssociatedProductModel();
        _productViewModelServiceMock.Setup(s => s.PrepareAssociatedProductModel()).ReturnsAsync(reprepared);
        var model = new ProductModel.AddAssociatedProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.AssociatedProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productViewModelServiceMock.Verify(s => s.InsertAssociatedProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>()), Times.Never);
    }
}
