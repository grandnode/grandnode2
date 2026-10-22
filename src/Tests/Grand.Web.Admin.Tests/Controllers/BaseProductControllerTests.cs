using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseProductControllerTests : ProductControllerTestsBase<BaseProductControllerTests.TestProductController>
{
    // BaseProductController is abstract - it has no host until Task 11 subclasses it. This
    // minimal subclass exists only so the actions under test can be invoked directly.
    public class TestProductController(
        IProductViewModelService productViewModelService,
        IProductAddPopupViewModelService productAddPopupViewModelService,
        IProductService productService,
        ILanguageService languageService,
        IRequestTranslationService translationService,
        IDateTimeService dateTimeService,
        IRequestPermissionService permissionService,
        IAdminDataScope<Product> scope)
        : BaseProductController(productViewModelService, productAddPopupViewModelService, productService, languageService, translationService,
            dateTimeService, permissionService, scope);

    protected override TestProductController CreateController() => new(
        _productViewModelServiceMock.Object,
        _productAddPopupViewModelServiceMock.Object,
        _productServiceMock.Object,
        _languageServiceMock.Object,
        _translationServiceMock.Object,
        _dateTimeServiceMock.Object,
        _permissionServiceMock.Object,
        _scopeMock.Object);

    // --- Edit (GET) --------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditGet_ProductNotFound_RedirectsToList()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", true)).ReturnsAsync((Product)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.CanView(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.Edit("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductModel(It.IsAny<ProductModel>(), It.IsAny<Product>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeGrantsView_ShowsForm()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductModel(It.IsAny<ProductModel>(), product, false, false), Times.Once);
    }

    [TestMethod]
    public async Task EditGet_Popup_ShowsThePopupView()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.Edit("p1", popup: true);

        Assert.AreEqual("EditPopup", ((ViewResult)result).ViewName);
    }

    [TestMethod]
    public async Task EditPost_Popup_ScopeDenies_StaysInThePopup()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.Edit(new ProductModel { Id = "p1" }, continueEditing: false, popup: true);

        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual(true, redirect.RouteValues["popup"]);
    }

    [TestMethod]
    public async Task EditGet_UsesCanViewNotHasAccess_LooserRuleWins()
    {
        // Regression guard for the review fix: Edit(GET) must gate on the looser CanView, not the
        // strict mutation-only HasAccess. A product HasAccess would deny (e.g. Store's multi-store
        // rule) but CanView allows must still show the form.
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        //shown, but read-only: HasAccess no longer gates the page, it only takes the buttons away
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(true, ((ViewResult)result).ViewData["IsReadOnly"]);
    }

    [TestMethod]
    public async Task EditGet_OwnedProduct_IsNotReadOnly()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(false, ((ViewResult)result).ViewData["IsReadOnly"]);
    }

    // --- Edit (POST) -------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditPost_ProductNotFound_RedirectsToList()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", true)).ReturnsAsync((Product)null);

        var result = await _controller.Edit(new ProductModel { Id = "missing" }, continueEditing: false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductModel(It.IsAny<Product>(), It.IsAny<ProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_ScopeDeniesAccess_RedirectsToEditWithoutUpdating()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.Edit(new ProductModel { Id = "p1" }, continueEditing: false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p1", redirect.RouteValues["id"]);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductModel(It.IsAny<Product>(), It.IsAny<ProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_ScopeGrantsAccess_Updates()
    {
        var product = new Product { Id = "p1", Ticks = 5 };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.UpdateProductModel(product, It.IsAny<ProductModel>()))
            .ReturnsAsync(product);

        var result = await _controller.Edit(new ProductModel { Id = "p1", Ticks = 5 }, continueEditing: false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.UpdateProductModel(product, It.IsAny<ProductModel>()), Times.Once);
    }

    // --- Delete --------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Delete_ProductNotFound_RedirectsToList()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", true)).ReturnsAsync((Product)null);

        var result = await _controller.Delete("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditWithoutDeleting()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p1", redirect.RouteValues["id"]);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ScopeGrantsAccess_DeletesAndRedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Once);
    }

    // --- CopyProduct -----------------------------------------------------------------------------------

    [TestMethod]
    public async Task CopyProduct_ScopeDeniesView_RedirectsToListWithoutCopying()
    {
        var originalProduct = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(originalProduct);
        _scopeMock.Setup(s => s.CanView(originalProduct)).ReturnsAsync(false);
        var copyProductServiceMock = new Mock<ICopyProductService>();

        var model = new ProductModel {
            CopyProductModel = new CopyProductModel { Id = "p1", Name = "copy" }
        };

        var result = await _controller.CopyProduct(model, copyProductServiceMock.Object, new Mock<IPictureService>().Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        copyProductServiceMock.Verify(
            s => s.CopyProduct(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task CopyProduct_ScopeGrantsView_Copies()
    {
        var originalProduct = new Product { Id = "p1" };
        var newProduct = new Product { Id = "p2" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(originalProduct);
        _scopeMock.Setup(s => s.CanView(originalProduct)).ReturnsAsync(true);
        var copyProductServiceMock = new Mock<ICopyProductService>();
        copyProductServiceMock.Setup(s => s.CopyProduct(originalProduct, "copy", false)).ReturnsAsync(newProduct);

        var model = new ProductModel {
            CopyProductModel = new CopyProductModel { Id = "p1", Name = "copy", Published = false, CopyImages = false }
        };

        var result = await _controller.CopyProduct(model, copyProductServiceMock.Object, new Mock<IPictureService>().Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p2", redirect.RouteValues["id"]);
    }

    [TestMethod]
    public async Task CopyProduct_UsesCanViewNotHasAccess_LooserRuleWins()
    {
        // Regression guard: CopyProduct must gate on CanView (Store's original rule: denies only
        // when LimitedToStores excludes the staff store), not the strict HasAccess.
        var originalProduct = new Product { Id = "p1" };
        var newProduct = new Product { Id = "p2" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(originalProduct);
        _scopeMock.Setup(s => s.HasAccess(originalProduct)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.CanView(originalProduct)).ReturnsAsync(true);
        var copyProductServiceMock = new Mock<ICopyProductService>();
        copyProductServiceMock.Setup(s => s.CopyProduct(originalProduct, "copy", false)).ReturnsAsync(newProduct);

        var model = new ProductModel {
            CopyProductModel = new CopyProductModel { Id = "p1", Name = "copy", Published = false, CopyImages = false }
        };

        var result = await _controller.CopyProduct(model, copyProductServiceMock.Object, new Mock<IPictureService>().Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p2", redirect.RouteValues["id"]);
    }

    // --- DeleteSelected ------------------------------------------------------------------------------

    [TestMethod]
    public async Task DeleteSelected_NoIds_DoesNotCallService()
    {
        var result = await _controller.DeleteSelected(new List<string>());

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        _productViewModelServiceMock.Verify(s => s.DeleteSelected(It.IsAny<List<string>>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteSelected_FiltersOutProductsScopeDenies()
    {
        // Regression guard for the review fix: DeleteSelected is a mutation, so it must filter through
        // the strict HasAccess before delegating - without this, Store would gain an unscoped
        // bulk-delete endpoint (any staff could delete any product id in the system).
        var owned = new Product { Id = "owned" };
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" },
            CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { owned, foreign });
        _scopeMock.Setup(s => s.HasAccess(owned)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);

        await _controller.DeleteSelected(new List<string> { "owned", "foreign" });

        _productViewModelServiceMock.Verify(
            s => s.DeleteSelected(It.Is<List<string>>(ids => ids.Count == 1 && ids[0] == "owned")), Times.Once);
    }

    [TestMethod]
    public async Task DeleteSelected_AllProductsScopeDenies_DoesNotCallDeleteSelected()
    {
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "foreign" }, CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { foreign });
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);

        await _controller.DeleteSelected(new List<string> { "foreign" });

        _productViewModelServiceMock.Verify(s => s.DeleteSelected(It.IsAny<List<string>>()), Times.Never);
    }

    // --- GoToSku -----------------------------------------------------------------------------------------
    // Deliberate behavior tightening vs. Store's pre-refactor GoToSku (see the TODO in
    // BaseProductController.GoToSku): denial now redirects to List, not Edit.

    [TestMethod]
    public async Task GoToSku_ProductNotFound_ShowsWarningAndRedirectsToList()
    {
        _productServiceMock.Setup(p => p.GetProductBySku("sku1")).ReturnsAsync((Product)null);

        var result = await _controller.GoToSku(new ProductListModel { GoDirectlyToSku = "sku1" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Product", redirect.ControllerName);
    }

    [TestMethod]
    public async Task GoToSku_ScopeDeniesAccess_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductBySku("sku1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.GoToSku(new ProductListModel { GoDirectlyToSku = "sku1" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Product", redirect.ControllerName);
    }

    [TestMethod]
    public async Task GoToSku_ScopeGrantsAccess_RedirectsToEdit()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductBySku("sku1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.GoToSku(new ProductListModel { GoDirectlyToSku = "sku1" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p1", redirect.RouteValues["id"]);
    }

    // --- List / Create default store-scoping -----------------------------------------------------------

    [TestMethod]
    public async Task List_CallsPrepareProductListModel()
    {
        // PrepareProductListModel() now reads scope.DefaultStoreId internally (Task 9) - the controller
        // no longer threads it through as an argument.
        _productViewModelServiceMock.Setup(s => s.PrepareProductListModel()).ReturnsAsync(new ProductListModel());

        var result = await _controller.List();

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductListModel(), Times.Once);
    }

    [TestMethod]
    public async Task Create_Get_DefaultsModelStoreIdFromScope()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");

        var result = await _controller.Create() as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("store-1", model.StoreId);
    }

    // --- LoadProductFriendlyNames --------------------------------------------------------------------
    // Filters the display list rather than denying the whole request: matches Store's CanAccessProduct
    // loop and Vendor's HasAccessToProduct loop, both of which skip inaccessible products silently.

    [TestMethod]
    public async Task LoadProductFriendlyNames_EmptyInput_ReturnsEmptyText()
    {
        var result = await _controller.LoadProductFriendlyNames("");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        Assert.AreEqual("", GetTextProperty(json.Value));
        _productServiceMock.Verify(s => s.GetProductsByIds(It.IsAny<string[]>(),
            It.IsAny<CatalogAccess>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadProductFriendlyNames_ScopeDeniesAccess_SkipsProduct()
    {
        var allowed = new Product { Id = "p1", Name = "Allowed" };
        var denied = new Product { Id = "p2", Name = "Denied" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "p1", "p2" }, CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { allowed, denied });
        _scopeMock.Setup(s => s.HasAccess(allowed)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(denied)).ReturnsAsync(false);

        var result = await _controller.LoadProductFriendlyNames("p1,p2");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        // Faithful port of Store/Vendor's original index-based comma logic: it decides whether to
        // append ", " from the loop index (i != products.Count - 1), not from whether a name was
        // actually appended, so skipping the last product still leaves a trailing ", ". Pre-existing
        // quirk in both original hosts, not introduced by this migration - characterized, not fixed.
        Assert.AreEqual("Allowed, ", GetTextProperty(json.Value));
    }

    [TestMethod]
    public async Task LoadProductFriendlyNames_ScopeGrantsAccess_IncludesAllProducts()
    {
        var p1 = new Product { Id = "p1", Name = "First" };
        var p2 = new Product { Id = "p2", Name = "Second" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "p1", "p2" }, CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { p1, p2 });
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<Product>())).ReturnsAsync(true);

        var result = await _controller.LoadProductFriendlyNames("p1,p2");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        Assert.AreEqual("First, Second", GetTextProperty(json.Value));
    }

    private static string GetTextProperty(object value) =>
        (string)value.GetType().GetProperty("Text")!.GetValue(value);

    // --- RequiredProductAddPopup ----------------------------------------------------------------------

    [TestMethod]
    public async Task RequiredProductAddPopup_CallsPrepareAddRequiredProductModel()
    {
        // PrepareAddRequiredProductModel() now reads scope internally (Task 9).
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareAddRequiredProductModel())
            .ReturnsAsync(new ProductModel.AddRequiredProductModel());

        var result = await _controller.RequiredProductAddPopup("input1") as ViewResult;

        Assert.IsNotNull(result);
        _productAddPopupViewModelServiceMock.Verify(s => s.PrepareAddRequiredProductModel(), Times.Once);
        Assert.AreEqual("input1", _controller.ViewBag.productIdsInput);
    }

    // --- RequiredProductAddPopupList -------------------------------------------------------------------

    [TestMethod]
    public async Task RequiredProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRequiredProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRequiredProductModel();
        var result = await _controller.RequiredProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task RequiredProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRequiredProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRequiredProductModel { SearchStoreId = "explicit" };
        var result = await _controller.RequiredProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- Product pictures ---------------------------------------------------------------------------
    // ProductPictureAdd is deliberately not covered here (same rationale as Store's original
    // ProductControllerTests): reaching its HasAccess check requires a non-empty IFormFileCollection and
    // a prior Pictures-permission check via IRequestPermissionService, disproportionate setup for what is
    // otherwise the same one-line HasAccess condition covered everywhere else in this region.

    // --- Product specification attributes -----------------------------------------------------------
    // GetOptionsByAttributeId is deliberately not covered here (same rationale as Product pictures'
    // ProductPictureAdd note): identical, unscoped across all three hosts, no access check involved.

    // --- Purchased with order ------------------------------------------------------------------------
    // Covers Admin and Store only - Vendor cannot bind to this signature (see the region comment in
    // BaseProductController.cs: Vendor has its own, structurally different IOrderViewModelService and
    // OrderListModel types).

    [TestMethod]
    public async Task PurchasedWithOrders_PermissionDenied_ReturnsEmptyGrid_DoesNotLoadProduct()
    {
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageOrders)).ReturnsAsync(false);
        var orderViewModelServiceMock = new Mock<IOrderViewModelService>();

        var result = await _controller.PurchasedWithOrders(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1", orderViewModelServiceMock.Object);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(0, gridModel.Total);
        Assert.IsNull(gridModel.Data);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task PurchasedWithOrders_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        var orderViewModelServiceMock = new Mock<IOrderViewModelService>();

        var result = await _controller.PurchasedWithOrders(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1", orderViewModelServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        orderViewModelServiceMock.Verify(
            s => s.PrepareOrderModel(It.IsAny<OrderListModel>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task PurchasedWithOrders_ScopeGrantsAccess_UsesDefaultStoreIdAndReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        // DefaultStoreId stands in for Store's original model.StoreId = StaffStoreId (and for Admin's
        // original, which left StoreId unset/null - GlobalAdminDataScope.DefaultStoreId is null).
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var orders = new List<OrderModel> { new() { Id = "order1" } };
        var orderViewModelServiceMock = new Mock<IOrderViewModelService>();
        orderViewModelServiceMock
            .Setup(s => s.PrepareOrderModel(
                It.Is<OrderListModel>(m => m.ProductId == "p1" && m.StoreId == "store-1"), 1, 10))
            .ReturnsAsync((orders, orders.Count));

        var result = await _controller.PurchasedWithOrders(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, "p1",
            orderViewModelServiceMock.Object);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- Reviews ---------------------------------------------------------------------------------

    [TestMethod]
    public async Task Reviews_ScopeDeniesAccess_ReturnsErrorJson_DoesNotLoadReviews()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        var productReviewServiceMock = new Mock<IProductReviewService>();

        var result = await _controller.Reviews(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1", productReviewServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        productReviewServiceMock.Verify(
            s => s.GetAllProductReviews(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Reviews_ScopeGrantsAccess_WithDefaultStoreId_FiltersReviewsByStore()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        // DefaultStoreId stands in for Store's original storeId argument (the staff member's
        // StaffStoreId, used to filter reviews to that store).
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var review = new ProductReview { Id = "r1", ProductId = "p1" };
        var reviews = new PagedList<ProductReview>(new List<ProductReview> { review }, 0, int.MaxValue);
        var productReviewServiceMock = new Mock<IProductReviewService>();
        productReviewServiceMock
            .Setup(s => s.GetAllProductReviews("", null, null, null, "", "store-1", "p1", 0, int.MaxValue))
            .ReturnsAsync(reviews);

        var result = await _controller.Reviews(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1", productReviewServiceMock.Object);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductReviewModel(It.IsAny<ProductReviewModel>(), review, false, true), Times.Once);
    }

    [TestMethod]
    public async Task Reviews_ScopeGrantsAccess_NullDefaultStoreId_PassesEmptyStoreId()
    {
        // Matches both Admin's and Vendor's originals, which both passed "" literally (Vendor scopes by
        // VendorId via the HasAccess check above, not by store - VendorProductDataScope.DefaultStoreId
        // is null).
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var reviews = new PagedList<ProductReview>(new List<ProductReview>(), 0, int.MaxValue);
        var productReviewServiceMock = new Mock<IProductReviewService>();
        productReviewServiceMock
            .Setup(s => s.GetAllProductReviews("", null, null, null, "", "", "p1", 0, int.MaxValue))
            .ReturnsAsync(reviews);

        var result = await _controller.Reviews(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1", productReviewServiceMock.Object);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(0, gridModel.Total);
        productReviewServiceMock.Verify(
            s => s.GetAllProductReviews("", null, null, null, "", "", "p1", 0, int.MaxValue), Times.Once);
    }

    // --- Export / Import ---------------------------------------------------------------------------

    [TestMethod]
    public async Task ExportExcelAll_Success_ReturnsXlsxFile()
    {
        var model = new ProductListModel();
        var products = new List<Product> { new() { Id = "p1" } };
        // vendorId is forced inside PrepareProducts itself; storeId is forced at the call site below,
        // same as every other PrepareProductsModel/PrepareProducts caller in this controller.
        _productViewModelServiceMock.Setup(s => s.PrepareProducts(model)).ReturnsAsync(products);
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock.Setup(e => e.Export(products)).ReturnsAsync([1, 2, 3]);

        var result = await _controller.ExportExcelAll(model, exportManagerMock.Object);

        var file = result as FileContentResult;
        Assert.IsNotNull(file);
        Assert.AreEqual("text/xls", file.ContentType);
        Assert.AreEqual("products.xlsx", file.FileDownloadName);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, file.FileContents);
    }

    [TestMethod]
    public async Task ExportExcelAll_UsesScopeDefaultStoreId()
    {
        // Regression test: found during a post-Phase-2 tenant-isolation audit that this action was the
        // one PrepareProducts/PrepareProductsModel caller in the controller NOT forcing
        // model.SearchStoreId = scope.DefaultStoreId, letting a Store-scoped user export other stores'
        // products by posting an arbitrary SearchStoreId.
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var model = new ProductListModel();
        _productViewModelServiceMock.Setup(s => s.PrepareProducts(model)).ReturnsAsync([]);
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock.Setup(e => e.Export(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);

        await _controller.ExportExcelAll(model, exportManagerMock.Object);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ExportExcelAll_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var model = new ProductListModel { SearchStoreId = "explicit" };
        _productViewModelServiceMock.Setup(s => s.PrepareProducts(model)).ReturnsAsync([]);
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock.Setup(e => e.Export(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);

        await _controller.ExportExcelAll(model, exportManagerMock.Object);

        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ExportExcelAll_ExportThrows_ReturnsRedirectToList()
    {
        var model = new ProductListModel();
        _productViewModelServiceMock.Setup(s => s.PrepareProducts(model)).ReturnsAsync([]);
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock.Setup(e => e.Export(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await _controller.ExportExcelAll(model, exportManagerMock.Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ExportExcelSelected_NullSelectedIds_ExportsEmptyList()
    {
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock.Setup(e => e.Export(It.Is<IEnumerable<Product>>(p => !p.Any()), It.IsAny<CancellationToken>()))
            .ReturnsAsync([9]);

        var result = await _controller.ExportExcelSelected(null, exportManagerMock.Object);

        var file = result as FileContentResult;
        Assert.IsNotNull(file);
        _productServiceMock.Verify(p => p.GetProductsByIds(It.IsAny<string[]>(),
            It.IsAny<CatalogAccess>()), Times.Never);
    }

    [TestMethod]
    public async Task ExportExcelSelected_FiltersOutProductsScopeDenies()
    {
        // Mirrors Vendor's original explicit HasAccessToProduct re-check on selectedIds (caller-supplied,
        // not derived from a scoped search, unlike ExportExcelAll) - applied unconditionally here so
        // Admin (where the original had no check at all) gets the same protection.
        var owned = new Product { Id = "owned" };
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" },
            CatalogAccess.IncludingHidden()))
            .ReturnsAsync([owned, foreign]);
        _scopeMock.Setup(s => s.HasAccess(owned)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock
            .Setup(e => e.Export(It.Is<IEnumerable<Product>>(p => p.Single() == owned), It.IsAny<CancellationToken>()))
            .ReturnsAsync([7]);

        var result = await _controller.ExportExcelSelected("owned,foreign", exportManagerMock.Object);

        var file = result as FileContentResult;
        Assert.IsNotNull(file);
        exportManagerMock.Verify(
            e => e.Export(It.Is<IEnumerable<Product>>(p => p.Single() == owned), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ImportExcel_EmptyFile_DoesNotImport_RedirectsToListWithError()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);
        var importManagerMock = new Mock<IImportManager<ProductDto>>();

        var result = await _controller.ImportExcel(fileMock.Object, importManagerMock.Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        importManagerMock.Verify(i => i.Import(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Common.UploadFile"), Times.Once);
    }

    // --- ImportExcel: global scope only ---

    [TestMethod]
    public async Task ImportExcel_StoreScope_DeniesAndNeverCallsImportManager()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        var importManagerMock = new Mock<IImportManager<ProductDto>>();

        var result = await _controller.ImportExcel(fileMock.Object, importManagerMock.Object);

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual("List", redirect.ActionName);
        importManagerMock.Verify(i => i.Import(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        _translationServiceMock.Verify(t => t.GetResource("Admin.AccessDenied.Title"), Times.Once);
    }

    [TestMethod]
    public async Task ImportExcel_VendorScope_DeniesAndNeverCallsImportManager()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-1");
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        var importManagerMock = new Mock<IImportManager<ProductDto>>();

        var result = await _controller.ImportExcel(fileMock.Object, importManagerMock.Object);

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual("List", redirect.ActionName);
        importManagerMock.Verify(i => i.Import(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        _translationServiceMock.Verify(t => t.GetResource("Admin.AccessDenied.Title"), Times.Once);
    }

    [TestMethod]
    public async Task ImportExcel_NullFile_DoesNotImport_RedirectsToListWithError()
    {
        var importManagerMock = new Mock<IImportManager<ProductDto>>();

        var result = await _controller.ImportExcel(null, importManagerMock.Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        importManagerMock.Verify(i => i.Import(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ImportExcel_ValidFile_ImportsAndRedirectsToListWithSuccess()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(3);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        var importManagerMock = new Mock<IImportManager<ProductDto>>();
        importManagerMock.Setup(i => i.Import(stream)).Returns(Task.CompletedTask);

        var result = await _controller.ImportExcel(fileMock.Object, importManagerMock.Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        importManagerMock.Verify(i => i.Import(stream), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Imported"), Times.Once);
    }

    [TestMethod]
    public async Task ImportExcel_ImportThrows_RedirectsToListWithError()
    {
        using var stream = new MemoryStream([1]);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        var importManagerMock = new Mock<IImportManager<ProductDto>>();
        importManagerMock.Setup(i => i.Import(stream)).ThrowsAsync(new Exception("bad file"));

        var result = await _controller.ImportExcel(fileMock.Object, importManagerMock.Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    // --- Bulk editing --------------------------------------------------------------------------------

    [TestMethod]
    public async Task BulkEdit_CallsPrepareBulkEditListModel()
    {
        // PrepareBulkEditListModel() now reads scope internally (Task 9).
        _productViewModelServiceMock.Setup(s => s.PrepareBulkEditListModel())
            .ReturnsAsync(new BulkEditListModel());

        var result = await _controller.BulkEdit();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        _productViewModelServiceMock.Verify(s => s.PrepareBulkEditListModel(), Times.Once);
    }

    [TestMethod]
    public async Task BulkEditSelect_StoreScoped_SetsSearchStoreIdFromScope()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");
        var model = new BulkEditListModel();
        _productViewModelServiceMock
            .Setup(s => s.PrepareBulkEditProductModel(It.IsAny<BulkEditListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<BulkEditProductModel>(), 0));

        await _controller.BulkEditSelect(new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task BulkEditSelect_NotStoreScoped_LeavesSearchStoreIdUntouched()
    {
        var model = new BulkEditListModel { SearchStoreId = "preset" };
        _productViewModelServiceMock
            .Setup(s => s.PrepareBulkEditProductModel(It.IsAny<BulkEditListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<BulkEditProductModel>(), 0));

        await _controller.BulkEditSelect(new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("preset", model.SearchStoreId);
    }

    [TestMethod]
    public async Task BulkEditCreate_NullProducts_DoesNotCallService()
    {
        var result = await _controller.BulkEditCreate(null);

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        _productViewModelServiceMock.Verify(
            s => s.InsertBulkEdit(It.IsAny<IEnumerable<BulkEditProductModel>>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkEditCreate_PassesOnlyRowsWithAName()
    {
        await _controller.BulkEditCreate(new List<BulkEditProductModel> {
            new() { Id = "", Name = "New product" },
            new() { Id = "", Name = " " }
        });

        _productViewModelServiceMock.Verify(
            s => s.InsertBulkEdit(It.Is<List<BulkEditProductModel>>(
                l => l.Count == 1 && l[0].Name == "New product")), Times.Once);
    }

    [TestMethod]
    public async Task BulkEditUpdate_NullProducts_DoesNotCallService()
    {
        var result = await _controller.BulkEditUpdate(null);

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        _productViewModelServiceMock.Verify(
            s => s.UpdateBulkEdit(It.IsAny<IEnumerable<BulkEditProductModel>>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkEditUpdate_FiltersOutProductsScopeDenies()
    {
        // Regression guard: Admin's original had no ownership check at all on this bulk-mutate endpoint
        // (any client-supplied id list was updated unconditionally). Store's original filtered via
        // FilterValidProductsForStore/CanAccessProduct; Vendor's original filtered via
        // HasAccessToProduct inside its own service. Routing through scope.HasAccess here reproduces
        // that per-item gate uniformly.
        var owned = new Product { Id = "owned" };
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" },
            CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { owned, foreign });
        _scopeMock.Setup(s => s.HasAccess(owned)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);

        var ownedModel = new BulkEditProductModel { Id = "owned" };
        var foreignModel = new BulkEditProductModel { Id = "foreign" };

        await _controller.BulkEditUpdate(new List<BulkEditProductModel> { ownedModel, foreignModel });

        _productViewModelServiceMock.Verify(
            s => s.UpdateBulkEdit(It.Is<List<BulkEditProductModel>>(
                p => p.Count == 1 && p[0].Id == "owned")), Times.Once);
    }

    [TestMethod]
    public async Task BulkEditUpdate_AllProductsScopeDenies_DoesNotCallUpdateBulkEdit()
    {
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "foreign" }, CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { foreign });
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);

        await _controller.BulkEditUpdate(new List<BulkEditProductModel> { new() { Id = "foreign" } });

        _productViewModelServiceMock.Verify(
            s => s.UpdateBulkEdit(It.IsAny<IEnumerable<BulkEditProductModel>>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkEditDelete_NullProducts_DoesNotCallService()
    {
        var result = await _controller.BulkEditDelete(null);

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        _productViewModelServiceMock.Verify(
            s => s.DeleteBulkEdit(It.IsAny<IEnumerable<BulkEditProductModel>>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkEditDelete_FiltersOutProductsScopeDenies()
    {
        var owned = new Product { Id = "owned" };
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" },
            CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { owned, foreign });
        _scopeMock.Setup(s => s.HasAccess(owned)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);

        await _controller.BulkEditDelete(new List<BulkEditProductModel> {
            new() { Id = "owned" }, new() { Id = "foreign" }
        });

        _productViewModelServiceMock.Verify(
            s => s.DeleteBulkEdit(It.Is<List<BulkEditProductModel>>(
                p => p.Count == 1 && p[0].Id == "owned")), Times.Once);
    }

    [TestMethod]
    public async Task BulkEditDelete_AllProductsScopeDenies_DoesNotCallDeleteBulkEdit()
    {
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "foreign" }, CatalogAccess.IncludingHidden()))
            .ReturnsAsync(new List<Product> { foreign });
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);

        await _controller.BulkEditDelete(new List<BulkEditProductModel> { new() { Id = "foreign" } });

        _productViewModelServiceMock.Verify(
            s => s.DeleteBulkEdit(It.IsAny<IEnumerable<BulkEditProductModel>>()), Times.Never);
    }

    [TestMethod]
    public async Task BulkEditUpdate_ProductsWithEmptyIds_AreIgnoredWithoutServiceCall()
    {
        var result = await _controller.BulkEditUpdate(new List<BulkEditProductModel> { new() { Id = "" } });

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        _productServiceMock.Verify(p => p.GetProductsByIds(It.IsAny<string[]>(),
            It.IsAny<CatalogAccess>()), Times.Never);
        _productViewModelServiceMock.Verify(
            s => s.UpdateBulkEdit(It.IsAny<IEnumerable<BulkEditProductModel>>()), Times.Never);
    }
}
