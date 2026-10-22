using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
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
        IProductService productService,
        IInventoryManageService inventoryManageService,
        ILanguageService languageService,
        IRequestTranslationService translationService,
        IProductReservationService productReservationService,
        IAuctionService auctionService,
        IDateTimeService dateTimeService,
        IRequestPermissionService permissionService,
        IEnumTranslationService enumTranslationService,
        IAdminDataScope<Product> scope)
        : BaseProductController(productViewModelService, productService, inventoryManageService,
            languageService, translationService, productReservationService, auctionService,
            dateTimeService, permissionService, enumTranslationService, scope);

    protected override TestProductController CreateController() => new(
        _productViewModelServiceMock.Object,
        _productServiceMock.Object,
        _inventoryManageServiceMock.Object,
        _languageServiceMock.Object,
        _translationServiceMock.Object,
        _productReservationServiceMock.Object,
        _auctionServiceMock.Object,
        _dateTimeServiceMock.Object,
        _permissionServiceMock.Object,
        _enumTranslationServiceMock.Object,
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
        _productViewModelServiceMock.Setup(s => s.PrepareAddRequiredProductModel())
            .ReturnsAsync(new ProductModel.AddRequiredProductModel());

        var result = await _controller.RequiredProductAddPopup("input1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAddRequiredProductModel(), Times.Once);
        Assert.AreEqual("input1", _controller.ViewBag.productIdsInput);
    }

    // --- RequiredProductAddPopupList -------------------------------------------------------------------

    [TestMethod]
    public async Task RequiredProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock
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
        _productViewModelServiceMock
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

    // --- ProductPriceList -------------------------------------------------------------------------
    // HasAccess (strict), not CanView: mirrors Store's CanAccessProduct check on this action. Applying it
    // uniformly also closes real gaps on the mutate actions below: Store's original checked access only on
    // List/Insert (Update/Delete had no check at all), and Vendor's original checked access only on List
    // (Insert/Update/Delete had no check at all).

    [TestMethod]
    public async Task ProductPriceList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductPriceList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductPriceList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductPriceList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.ProductPriceList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductPriceInsert -----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPriceInsert_ScopeDeniesAccess_ReturnsErrorJson_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPriceModel { ProductId = "p1", CurrencyCode = "EUR", Price = 9.99 };

        var result = await _controller.ProductPriceInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.InsertProductPrice(It.IsAny<ProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceInsert_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPriceModel { ProductId = "p1", CurrencyCode = "EUR", Price = 9.99 };

        var result = await _controller.ProductPriceInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.InsertProductPrice(It.Is<ProductPrice>(
            pp => pp.ProductId == "p1" && pp.CurrencyCode == "EUR" && pp.Price == 9.99)), Times.Once);
    }

    // --- ProductPriceUpdate -----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPriceUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1", CurrencyCode = "USD", Price = 19.99 };

        var result = await _controller.ProductPriceUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.UpdateProductPrice(It.IsAny<ProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1", CurrencyCode = "USD", Price = 19.99 };

        var result = await _controller.ProductPriceUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.UpdateProductPrice(It.Is<ProductPrice>(
            pp => pp.Id == "pp1" && pp.CurrencyCode == "USD" && pp.Price == 19.99 && pp.ProductId == "p1")), Times.Once);
    }

    // --- ProductPriceDelete -----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPriceDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1" };

        var result = await _controller.ProductPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.DeleteProductPrice(It.IsAny<ProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1" };

        var result = await _controller.ProductPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.DeleteProductPrice(It.Is<ProductPrice>(
            pp => pp.Id == "pp1" && pp.ProductId == "p1")), Times.Once);
    }

    // --- TierPriceList ----------------------------------------------------------------------------
    // HasAccess applied uniformly on every action in this region. Vendor's original checked ownership
    // only on List and TierPriceEditPopup(GET); TierPriceCreatePopup(POST), TierPriceEditPopup(POST) and
    // TierPriceDelete had NO ownership check at all, letting a vendor create/update/delete a tier price on
    // any product by id. Store's original never checked TierPriceEditPopup(GET) either.

    [TestMethod]
    public async Task TierPriceList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceList_ScopeGrantsAccess_ReturnsGrid()
    {
        // PrepareTierPriceModel(product) now reads scope internally (Task 9).
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        var tierPrices = new List<ProductModel.TierPriceModel> { new() { Id = "tp1" } };
        _productViewModelServiceMock.Setup(s => s.PrepareTierPriceModel(product)).ReturnsAsync(tierPrices);

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- TierPriceCreatePopup (GET) ----------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceCreatePopup_Get_PreparesModel()
    {
        // PrepareTierPriceModel(model) now reads scope internally (Task 9).
        var result = await _controller.TierPriceCreatePopup("p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.Is<ProductModel.TierPriceModel>(m => m.ProductId == "p1")),
            Times.Once);
    }

    // --- TierPriceCreatePopup (POST) ---------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceCreatePopup_Post_ScopeDeniesAccess_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.TierPriceModel { ProductId = "p1" };

        var result = await _controller.TierPriceCreatePopup(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.InsertTierPrice(It.IsAny<TierPrice>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceCreatePopup_Post_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceModel { ProductId = "p1" };

        var result = await _controller.TierPriceCreatePopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productServiceMock.Verify(
            s => s.InsertTierPrice(It.IsAny<TierPrice>(), "p1"), Times.Once);
    }

    [TestMethod]
    public async Task TierPriceCreatePopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.TierPriceModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.TierPriceCreatePopup(model));
    }

    // --- TierPriceEditPopup (GET) ------------------------------------------------------------------
    // HasAccess added here: Store's original had no ownership check on this GET action at all.

    [TestMethod]
    public async Task TierPriceEditPopup_Get_ScopeDeniesAccess_ReturnsNotYourProductContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.TierPriceEditPopup("tp1", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your product", content.Content);
        _productViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.IsAny<ProductModel.TierPriceModel>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Get_ScopeGrantsAccess_TierPriceMissing_ReturnsEmptyContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.TierPriceEditPopup("missing-tp", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Empty tier price", content.Content);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        // PrepareTierPriceModel(model) now reads scope internally (Task 9).
        var product = new Product { Id = "p1" };
        product.TierPrices.Add(new TierPrice { Id = "tp1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.TierPriceEditPopup("tp1", "p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.Is<ProductModel.TierPriceModel>(m => m.ProductId == "p1")),
            Times.Once);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Get_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.TierPriceEditPopup("tp1", "missing"));
    }

    // --- TierPriceEditPopup (POST) -----------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceEditPopup_Post_ScopeDeniesAccess_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.TierPriceModel { Id = "tp1", ProductId = "p1" };

        var result = await _controller.TierPriceEditPopup("p1", model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.UpdateTierPrice(It.IsAny<TierPrice>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Post_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        product.TierPrices.Add(new TierPrice { Id = "tp1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceModel { Id = "tp1", ProductId = "p1" };

        var result = await _controller.TierPriceEditPopup("p1", model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productServiceMock.Verify(s => s.UpdateTierPrice(It.IsAny<TierPrice>(), "p1"), Times.Once);
    }

    // --- TierPriceDelete ----------------------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceDelete_ScopeDeniesAccess_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.TierPriceDeleteModel("tp1", "p1");

        var result = await _controller.TierPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.DeleteTierPrice(It.IsAny<TierPrice>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.TierPrices.Add(new TierPrice { Id = "tp1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceDeleteModel("tp1", "p1");

        var result = await _controller.TierPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.DeleteTierPrice(It.IsAny<TierPrice>(), "p1"), Times.Once);
    }

    [TestMethod]
    public async Task TierPriceDelete_ScopeGrantsAccess_TierPriceMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceDeleteModel("missing-tp", "p1");

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.TierPriceDelete(model));
    }

    // --- ProductAttributeMappingList -------------------------------------------------------------
    // HasAccess applied uniformly on every action in this region. Store's original checked ownership
    // (CanAccessProduct) on List/PopupGET/PopupPOST/Delete/ValidationRulesPopupGET but not on
    // ValidationRulesPopup(POST). Vendor's original checked (CheckAccessToProduct/HasAccessToProduct) on
    // List/PopupGET/Delete/ValidationRulesPopupGET, but NOT on ProductAttributeMappingPopup(POST) or
    // ValidationRulesPopup(POST) - letting a vendor edit an attribute mapping (or its validation rules) on
    // any product, not just their own, by posting a known productId.

    [TestMethod]
    public async Task ProductAttributeMappingList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeMappingList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeMappingModels(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductAttributeMappingList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        var attributes = new List<ProductModel.ProductAttributeMappingModel> { new() { Id = "pam1" } };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModels(product))
            .ReturnsAsync(attributes);

        var result = await _controller.ProductAttributeMappingList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeMappingPopup (GET) ------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeMappingPopup("p1", null);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeMappingModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Get_ScopeGrantsAccess_NoMappingId_PreparesNewModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(product))
            .ReturnsAsync(new ProductModel.ProductAttributeMappingModel());

        var result = await _controller.ProductAttributeMappingPopup("p1", null);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeMappingModel(product), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Get_ScopeGrantsAccess_MappingId_PreparesEditModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(product, mapping))
            .ReturnsAsync(new ProductModel.ProductAttributeMappingModel());

        var result = await _controller.ProductAttributeMappingPopup("p1", "pam1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeMappingModel(product, mapping), Times.Once);
    }

    // --- ProductAttributeMappingPopup (POST) -----------------------------------------------------
    // HasAccess added here: Vendor's original had no ownership check on this POST at all.

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_ScopeDeniesAccess_DoesNotInsertOrUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeMappingPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeMappingModel(It.IsAny<ProductModel.ProductAttributeMappingModel>()),
            Times.Never);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeMappingModel(It.IsAny<ProductModel.ProductAttributeMappingModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_ScopeGrantsAccess_NoId_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeMappingPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.InsertProductAttributeMappingModel(model), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeMappingModel(It.IsAny<ProductModel.ProductAttributeMappingModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_ScopeGrantsAccess_WithId_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };

        var result = await _controller.ProductAttributeMappingPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductAttributeMappingModel(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductAttributeMappingPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_InvalidModelState_ReturnsView_DoesNotCheckAccess()
    {
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "p1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(model)).ReturnsAsync(model);

        var result = await _controller.ProductAttributeMappingPopup(model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    // --- ProductAttributeMappingDelete -----------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeMappingDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeMappingDelete("pam1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeMappingDelete_MappingNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeMappingDelete("missing-pam", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeMappingDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeMappingDelete("pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeMapping(It.IsAny<ProductAttributeMapping>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingDelete_ScopeGrantsAccess_Deletes()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeMappingDelete("pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(s => s.DeleteProductAttributeMapping(mapping, "p1"), Times.Once);
    }

    // --- ProductAttributeValidationRulesPopup (GET) -----------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValidationRulesPopup("pam1", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Get_ScopeGrantsAccess_MappingMissing_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeValidationRulesPopup("missing-pam", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("No attribute value found with the specified id", content.Content);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(mapping))
            .ReturnsAsync(new ProductModel.ProductAttributeMappingModel());

        var result = await _controller.ProductAttributeValidationRulesPopup("pam1", "p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeMappingModel(mapping), Times.Once);
    }

    // --- ProductAttributeValidationRulesPopup (POST) ----------------------------------------------
    // HasAccess added here: none of the three original hosts checked ownership on this POST at all - a
    // store/vendor user could update an attribute mapping's validation rules on any product by posting a
    // known productId/model.Id.

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValidationRulesPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_MappingNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "missing-pam", ProductId = "p1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValidationRulesPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotUpdate()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };

        var result = await _controller.ProductAttributeValidationRulesPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValidationRulesModel(It.IsAny<ProductAttributeMapping>(),
                It.IsAny<ProductModel.ProductAttributeMappingModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_ScopeGrantsAccess_ValidModel_Updates()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };

        var result = await _controller.ProductAttributeValidationRulesPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValidationRulesModel(mapping, model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_ScopeGrantsAccess_InvalidModelState_ReturnsView()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(mapping)).ReturnsAsync(model);

        var result = await _controller.ProductAttributeValidationRulesPopup(model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValidationRulesModel(It.IsAny<ProductAttributeMapping>(),
                It.IsAny<ProductModel.ProductAttributeMappingModel>()), Times.Never);
    }

    // --- ProductAttributeConditionPopup (GET) ----------------------------------------------------
    // HasAccess added uniformly. Admin's original had no ownership check on either action of this
    // region at all.

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeConditionPopup("p1", "pam1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeConditionModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductAttributeConditionPopup("p1", "pam1");

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeGrantsAccess_MappingMissing_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeConditionPopup("p1", "missing-pam");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("No attribute value found with the specified id", content.Content);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeConditionModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeConditionModel(product, mapping))
            .ReturnsAsync(new ProductAttributeConditionModel());

        var result = await _controller.ProductAttributeConditionPopup("p1", "pam1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeConditionModel(product, mapping),
            Times.Once);
    }

    // --- ProductAttributeConditionPopup (POST) ---------------------------------------------------
    // HasAccess added here: Vendor's original never checked ownership on this POST at all (only its
    // GET sibling did, via CheckAccessToProduct), letting a vendor update an attribute condition on
    // any product by posting a known productId/productAttributeMappingId. Admin's original had no
    // check on either action.

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductAttributeConditionModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductAttributeConditionPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_MappingNotFound_ReturnsContent_DoesNotCheckAccess()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var model = new ProductAttributeConditionModel { ProductId = "p1", ProductAttributeMappingId = "missing-pam" };

        var result = await _controller.ProductAttributeConditionPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("No attribute value found with the specified id", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotUpdate()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductAttributeConditionModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeConditionPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeConditionModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>(),
                It.IsAny<ProductAttributeConditionModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_ScopeGrantsAccess_Updates()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeConditionModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeConditionPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductAttributeConditionModel(product, mapping, model),
            Times.Once);
    }

    // --- EditAttributeValues (GET) ------------------------------------------------------------------
    // HasAccess applied uniformly. Admin's original had no ownership check on this action at all.

    [TestMethod]
    public async Task EditAttributeValues_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.EditAttributeValues("pam1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task EditAttributeValues_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.EditAttributeValues("pam1", "p1", attrServiceMock.Object);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task EditAttributeValues_ScopeGrantsAccess_MappingMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.EditAttributeValues("missing-pam", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task EditAttributeValues_ScopeGrantsAccess_PreparesListModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1", ProductAttributeId = "pa1" };
        var product = new Product { Id = "p1", Name = "Product 1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();
        attrServiceMock.Setup(s => s.GetProductAttributeById("pa1"))
            .ReturnsAsync(new ProductAttribute { Id = "pa1", Name = "Color" });

        var result = await _controller.EditAttributeValues("pam1", "p1", attrServiceMock.Object);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as ProductModel.ProductAttributeValueListModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        Assert.AreEqual("Product 1", model.ProductName);
        Assert.AreEqual("Color", model.ProductAttributeName);
        Assert.AreEqual("pam1", model.ProductAttributeMappingId);
    }

    // --- ProductAttributeValueList (POST) -----------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValueList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValueList("pam1", "p1", new Grand.Web.Common.DataSource.DataSourceRequest());

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeValueModels(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueList_ScopeGrantsAccess_MappingMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueList("missing-pam", "p1", new Grand.Web.Common.DataSource.DataSourceRequest()));
    }

    [TestMethod]
    public async Task ProductAttributeValueList_ScopeGrantsAccess_ReturnsGrid()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var values = new List<ProductModel.ProductAttributeValueModel> { new() { Id = "pav1" } };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeValueModels(product, mapping))
            .ReturnsAsync(values);

        var result = await _controller.ProductAttributeValueList("pam1", "p1", new Grand.Web.Common.DataSource.DataSourceRequest());

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeValueCreatePopup (GET) -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValueCreatePopup("pam1", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeValueModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Get_ScopeGrantsAccess_MappingMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueCreatePopup("missing-pam", "p1"));
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeValueModel(product, mapping))
            .ReturnsAsync(new ProductModel.ProductAttributeValueModel());

        var result = await _controller.ProductAttributeValueCreatePopup("pam1", "p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, mapping), Times.Once);
    }

    // --- ProductAttributeValueCreatePopup (POST) ----------------------------------------------------
    // HasAccess added explicitly. Admin has no ownership concept at all (GlobalAdminDataScope.HasAccess is
    // always true), so Admin was never at risk here. The real risk is to VENDOR: Vendor's original relied
    // solely on ProductAttributeValueModel : IProductValidVendor triggering ValidationFilter's
    // ProductValidVendor check + Vendor's own `if (ModelState.IsValid)` gate - no explicit action-level
    // check. Moving to the shared AdminShared model (no IProductValidVendor) would have silently dropped
    // that guard for Vendor; scope.HasAccess replaces it explicitly and uniformly.

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueCreatePopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeValueModel(It.IsAny<ProductModel.ProductAttributeValueModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeDeniesAccess_UsesVendorResourceKeyPrefix()
    {
        // The host/action combination this row's fix actually protects: Vendor losing its
        // IProductValidVendor-driven guard in the merge. See the comment above the HasAccess check in
        // BaseProductController.ProductAttributeValueCreatePopup(POST).
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeValueModel(It.IsAny<ProductModel.ProductAttributeValueModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeGrantsAccess_MappingMissing_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1", ProductAttributeMappingId = "missing-pam" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Product", redirect.ControllerName);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.InsertProductAttributeValueModel(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeGrantsAccess_InvalidModelState_ReturnsView()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeValueModel(It.IsAny<ProductModel.ProductAttributeValueModel>()),
            Times.Never);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, model), Times.Once);
    }

    // --- ProductAttributeValueEditPopup (GET) -------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValueEditPopup("pav1", "p1", "pam1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeGrantsAccess_MappingMissing_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeValueEditPopup("pav1", "p1", "missing-pam");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeGrantsAccess_ValueMissing_RedirectsToList()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeValueEditPopup("missing-pav", "p1", "pam1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel();
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeValueModel(mapping, pav))
            .ReturnsAsync(model);

        var result = await _controller.ProductAttributeValueEditPopup("pav1", "p1", "pam1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(mapping, pav), Times.Once);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, model), Times.Once);
    }

    // --- ProductAttributeValueEditPopup (POST) ------------------------------------------------------
    // HasAccess added here: Vendor's original never checked ownership on this POST at all (only its GET
    // sibling did); Admin's original had no check on either action.

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeValueModel();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueEditPopup("missing", model));
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeValueModel { Id = "pav1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValueModel(It.IsAny<ProductAttributeValue>(),
                It.IsAny<ProductModel.ProductAttributeValueModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeGrantsAccess_ValueMissing_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { Id = "missing-pav", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeGrantsAccess_ValidModel_Updates()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { Id = "pav1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductAttributeValueModel(pav, model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeGrantsAccess_InvalidModelState_ReturnsView()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeValueModel { Id = "pav1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValueModel(It.IsAny<ProductAttributeValue>(),
                It.IsAny<ProductModel.ProductAttributeValueModel>()), Times.Never);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, model), Times.Once);
    }

    // --- ProductAttributeValueDelete -----------------------------------------------------------------
    // HasAccess added here: this action takes only simple string parameters, so no model-level validator
    // ever runs for it. Admin's original had no ownership check at all.

    [TestMethod]
    public async Task ProductAttributeValueDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueDelete("pav1", "pam1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeValueDelete("pav1", "pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeValue(It.IsAny<ProductAttributeValue>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeGrantsAccess_ValueMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueDelete("missing-pav", "pam1", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeValueDelete("pav1", "pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(s => s.DeleteProductAttributeValue(pav, "p1", "pam1"), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeGrantsAccess_InvalidModelState_ReturnsErrorJson_DoesNotDelete()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeValueDelete("pav1", "pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeValue(It.IsAny<ProductAttributeValue>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    // --- AssociateProductToAttributeValuePopup (GET) ------------------------------------------------

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Get_CallsPrepareAssociateProductToAttributeValueModel()
    {
        // PrepareAssociateProductToAttributeValueModel() now reads scope internally (Task 9).
        _productViewModelServiceMock.Setup(s => s.PrepareAssociateProductToAttributeValueModel())
            .ReturnsAsync(new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel());

        var result = await _controller.AssociateProductToAttributeValuePopup();

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAssociateProductToAttributeValueModel(),
            Times.Once);
    }

    // --- AssociateProductToAttributeValuePopupList (POST) -------------------------------------------

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopupList_DefaultStoreIdSet_AppliesSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel();
        _productViewModelServiceMock.Setup(s => s.PrepareProductModel(model, 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var result = await _controller.AssociateProductToAttributeValuePopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopupList_DefaultStoreIdNull_LeavesSearchStoreIdUntouched()
    {
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { SearchStoreId = "preset" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductModel(model, 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var result = await _controller.AssociateProductToAttributeValuePopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("preset", model.SearchStoreId);
    }

    // --- AssociateProductToAttributeValuePopup (POST) -----------------------------------------------
    // HasAccess added here: Admin's original had no ownership check on the associated product at all.

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Post_AssociatedProductMissing_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { AssociatedToProductId = "missing" };

        var result = await _controller.AssociateProductToAttributeValuePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Cannot load a product", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { AssociatedToProductId = "p1" };

        var result = await _controller.AssociateProductToAttributeValuePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Post_ScopeGrantsAccess_ReturnsEmptyContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { AssociatedToProductId = "p1" };

        var result = await _controller.AssociateProductToAttributeValuePopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        var content = result as ContentResult;
        Assert.AreEqual("", content.Content);
    }

    // --- ProductAttributeCombinationList (POST) -------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationList(new DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeCombinationModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeCombinationModel(product))
            .ReturnsAsync(new List<ProductModel.ProductAttributeCombinationModel> { new() });

        var result = await _controller.ProductAttributeCombinationList(new DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as DataSourceResult;
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeCombinationDelete -------------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationDelete("c1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeCombinationDelete("c1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeCombination(It.IsAny<ProductAttributeCombination>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_ScopeGrantsAccess_CombinationMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationDelete("missing-c", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_ScopeGrantsAccess_Deletes()
    {
        var product = new Product { Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.DontManageStock };
        var combination = new ProductAttributeCombination { Id = "c1" };
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeCombinationDelete("c1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(s => s.DeleteProductAttributeCombination(combination, "p1"), Times.Once);
    }

    // --- AttributeCombinationPopup (GET) ---------------------------------------------------------------

    [TestMethod]
    public async Task AttributeCombinationPopupGet_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.AttributeCombinationPopup("p1", "c1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeCombinationModel(It.IsAny<Product>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupGet_ScopeGrantsAccess_ReturnsView()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeCombinationModel { Id = "c1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeCombinationModel(product, "c1"))
            .ReturnsAsync(model);

        var result = await _controller.AttributeCombinationPopup("p1", "c1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
        _productViewModelServiceMock.Verify(s => s.PrepareAddProductAttributeCombinationModel(model, product),
            Times.Once);
    }

    // --- AttributeCombinationPopup (POST) --------------------------------------------------------------

    [TestMethod]
    public async Task AttributeCombinationPopupPost_MissingProduct_RedirectsToList()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductAttributeCombinationModel();

        var result = await _controller.AttributeCombinationPopup("missing", model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupPost_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductAttributeCombinationModel();

        var result = await _controller.AttributeCombinationPopup("p1", model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertOrUpdateProductAttributeCombinationPopup(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombinationModel>()), Times.Never);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupPost_ScopeGrantsAccess_NoWarnings_ReturnsEmptyContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeCombinationModel();
        _productViewModelServiceMock.Setup(s => s.InsertOrUpdateProductAttributeCombinationPopup(product, model))
            .ReturnsAsync(new List<string>());

        var result = await _controller.AttributeCombinationPopup("p1", model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupPost_ScopeGrantsAccess_WithWarnings_ReturnsViewWithWarnings()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeCombinationModel();
        _productViewModelServiceMock.Setup(s => s.InsertOrUpdateProductAttributeCombinationPopup(product, model))
            .ReturnsAsync(new List<string> { "warning" });

        var result = await _controller.AttributeCombinationPopup("p1", model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
        CollectionAssert.Contains(model.Warnings.ToList(), "warning");
        _productViewModelServiceMock.Verify(s => s.PrepareAddProductAttributeCombinationModel(model, product),
            Times.Once);
    }

    // --- GenerateAllAttributeCombinations ---------------------------------------------------------------

    [TestMethod]
    public async Task GenerateAllAttributeCombinations_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.GenerateAllAttributeCombinations("missing"));
    }

    [TestMethod]
    public async Task GenerateAllAttributeCombinations_ScopeDeniesAccess_ReturnsErrorJson_DoesNotGenerate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.GenerateAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.GenerateAllAttributeCombinations(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task GenerateAllAttributeCombinations_ScopeGrantsAccess_Generates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.GenerateAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.GenerateAllAttributeCombinations(product), Times.Once);
    }

    // --- ClearAllAttributeCombinations -----------------------------------------------------------------

    [TestMethod]
    public async Task ClearAllAttributeCombinations_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ClearAllAttributeCombinations("missing"));
    }

    [TestMethod]
    public async Task ClearAllAttributeCombinations_ScopeDeniesAccess_ReturnsErrorJson_DoesNotClear()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ClearAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.ClearAllAttributeCombinations(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ClearAllAttributeCombinations_ScopeGrantsAccess_ValidModel_Clears()
    {
        var product = new Product
            { Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.DontManageStock };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ClearAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.ClearAllAttributeCombinations(product), Times.Once);
    }

    [TestMethod]
    public async Task ClearAllAttributeCombinations_ScopeGrantsAccess_InvalidModelState_ReturnsErrorJson_DoesNotClear()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");

        var result = await _controller.ClearAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.ClearAllAttributeCombinations(It.IsAny<Product>()), Times.Never);
    }

    // --- ProductAttributeCombinationTierPriceList (POST) -----------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceList(new DataSourceRequest(), "p1", "c1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeCombinationTierPricesModel(It.IsAny<Product>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeCombinationTierPricesModel(product, "c1"))
            .ReturnsAsync(new List<ProductModel.ProductAttributeCombinationTierPricesModel> { new() });

        var result = await _controller.ProductAttributeCombinationTierPriceList(new DataSourceRequest(), "p1", "c1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as DataSourceResult;
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeCombinationTierPriceInsert -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationTierPriceInsert("missing", "c1",
                new ProductModel.ProductAttributeCombinationTierPricesModel()));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_ScopeDeniesAccess_ReturnsContent_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceInsert("p1", "c1",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeCombinationTierPricesModel(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductModel.ProductAttributeCombinationTierPricesModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_ScopeGrantsAccess_CombinationMissing_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeCombinationTierPriceInsert("p1", "missing-c",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeCombinationTierPricesModel(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductModel.ProductAttributeCombinationTierPricesModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_ScopeGrantsAccess_Inserts()
    {
        var product = new Product { Id = "p1" };
        var combination = new ProductAttributeCombination { Id = "c1" };
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeCombinationTierPricesModel();

        var result = await _controller.ProductAttributeCombinationTierPriceInsert("p1", "c1", model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeCombinationTierPricesModel(product, combination, model), Times.Once);
    }

    // --- ProductAttributeCombinationTierPriceUpdate -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceUpdate_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationTierPriceUpdate("missing", "c1",
                new ProductModel.ProductAttributeCombinationTierPricesModel()));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceUpdate_ScopeDeniesAccess_ReturnsContent_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceUpdate("p1", "c1",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeCombinationTierPricesModel(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductModel.ProductAttributeCombinationTierPricesModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceUpdate_ScopeGrantsAccess_Updates()
    {
        var product = new Product { Id = "p1" };
        var combination = new ProductAttributeCombination { Id = "c1" };
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeCombinationTierPricesModel();

        var result = await _controller.ProductAttributeCombinationTierPriceUpdate("p1", "c1", model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeCombinationTierPricesModel(product, combination, model), Times.Once);
    }

    // --- ProductAttributeCombinationTierPriceDelete -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationTierPriceDelete("missing", "c1", "tp1"));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_ScopeDeniesAccess_ReturnsContent_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceDelete("p1", "c1", "tp1");

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductAttributeCombinationTierPrices(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductCombinationTierPrices>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_ScopeGrantsAccess_TierPriceMissing_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeCombinationTierPriceDelete("p1", "c1", "missing-tp");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductAttributeCombinationTierPrices(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductCombinationTierPrices>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_ScopeGrantsAccess_Deletes()
    {
        var product = new Product { Id = "p1" };
        var combination = new ProductAttributeCombination { Id = "c1" };
        var tierPrice = new ProductCombinationTierPrices { Id = "tp1" };
        combination.TierPrices.Add(tierPrice);
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeCombinationTierPriceDelete("p1", "c1", "tp1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductAttributeCombinationTierPrices(product, combination, tierPrice), Times.Once);
    }
}
