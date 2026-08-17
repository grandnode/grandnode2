using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

// Characterization tests for the merged access-check behavior in BaseProductController's
// "Product list / create / edit / delete" region (ARCH-001 Phase 1 Task 7). These replace the
// equivalent per-host access-check cases in Grand.Web.Admin/Store/Vendor.Tests ProductControllerTests
// (removed in Task 13), parameterized over a mocked IAdminDataScope<Product> instead of three
// different concrete access mechanisms (Admin: none: Store: AccessToEntityByStore; Vendor: HasAccessToProduct).
[TestClass]
public class BaseProductControllerTests
{
    // BaseProductController is abstract - it has no host until Task 11 subclasses it. This
    // minimal subclass exists only so the actions under test can be invoked directly.
    private class TestProductController(
        IProductViewModelService productViewModelService,
        IProductService productService,
        IInventoryManageService inventoryManageService,
        ILanguageService languageService,
        ITranslationService translationService,
        IProductReservationService productReservationService,
        IAuctionService auctionService,
        IDateTimeService dateTimeService,
        IPermissionService permissionService,
        IEnumTranslationService enumTranslationService,
        IAdminDataScope<Product> scope)
        : BaseProductController(productViewModelService, productService, inventoryManageService,
            languageService, translationService, productReservationService, auctionService,
            dateTimeService, permissionService, enumTranslationService, scope);

    private TestProductController _controller;
    private Mock<IProductService> _productServiceMock;
    private Mock<IProductViewModelService> _productViewModelServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<Product>> _scopeMock;
    private Mock<IPermissionService> _permissionServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<ProductSpecificationProfile>();
        });
        AutoMapperConfig.Init(mapperConfig);

        _productServiceMock = new Mock<IProductService>();
        _productViewModelServiceMock = new Mock<IProductViewModelService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Admin");
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        _permissionServiceMock = new Mock<IPermissionService>();
        _permissionServiceMock.Setup(p => p.Authorize(It.IsAny<Permission>())).ReturnsAsync(true);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Domain.Localization.Language>());

        _controller = new TestProductController(
            _productViewModelServiceMock.Object,
            _productServiceMock.Object,
            new Mock<IInventoryManageService>().Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IProductReservationService>().Object,
            new Mock<IAuctionService>().Object,
            new Mock<IDateTimeService>().Object,
            _permissionServiceMock.Object,
            new Mock<IEnumTranslationService>().Object,
            _scopeMock.Object);

        var httpContext = new DefaultHttpContext();
        // Needed for actions whose catch (Exception) block calls Error(exc), which logs via
        // HttpContext.RequestServices.GetRequiredService<ILoggerFactory>() (see Export / Import below).
        // Once RequestServices is non-null, ControllerBase.Url's own
        // HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>() call (used by every
        // RedirectToAction(action, controller) - e.g. GoToSku above) stops being short-circuited by the
        // null-conditional it uses when RequestServices itself is null, so IUrlHelperFactory must resolve
        // too or those pre-existing tests start throwing.
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

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

        Assert.IsInstanceOfType<ViewResult>(result);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
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
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" }, true))
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
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "foreign" }, true))
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
    public async Task List_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareProductListModel("store-1")).ReturnsAsync(new ProductListModel());

        var result = await _controller.List();

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductListModel("store-1"), Times.Once);
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
        _productServiceMock.Verify(s => s.GetProductsByIds(It.IsAny<string[]>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadProductFriendlyNames_ScopeDeniesAccess_SkipsProduct()
    {
        var allowed = new Product { Id = "p1", Name = "Allowed" };
        var denied = new Product { Id = "p2", Name = "Denied" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "p1", "p2" }, true))
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
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "p1", "p2" }, true))
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
    public async Task RequiredProductAddPopup_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareAddRequiredProductModel("store-1"))
            .ReturnsAsync(new ProductModel.AddRequiredProductModel());

        var result = await _controller.RequiredProductAddPopup("input1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAddRequiredProductModel("store-1"), Times.Once);
        Assert.AreEqual("input1", _controller.ViewBag.productIdsInput);
    }

    [TestMethod]
    public async Task RequiredProductAddPopup_NoDefaultStoreId_PassesEmptyString()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock.Setup(s => s.PrepareAddRequiredProductModel(""))
            .ReturnsAsync(new ProductModel.AddRequiredProductModel());

        var result = await _controller.RequiredProductAddPopup("input1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAddRequiredProductModel(""), Times.Once);
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

    // --- ProductCategoryList ----------------------------------------------------------------------
    // HasAccess (strict), not CanView: mirrors Store's CanAccessProduct (AccessToEntityByStore) and
    // Vendor's CheckAccessToProduct (VendorId equality) gating this action on both hosts.

    [TestMethod]
    public async Task ProductCategoryList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductCategoryList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.PrepareProductCategoryModel(It.IsAny<Product>()), Times.Never);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductCategoryModel(product))
            .ReturnsAsync(new List<ProductModel.ProductCategoryModel> { new() { Id = "c1", ProductId = "p1" } });

        var result = await _controller.ProductCategoryList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
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
        _productViewModelServiceMock.Verify(s => s.InsertProductCategoryModel(It.IsAny<ProductModel.ProductCategoryModel>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.InsertProductCategoryModel(model), Times.Once);
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
        _productViewModelServiceMock.Verify(s => s.UpdateProductCategoryModel(It.IsAny<ProductModel.ProductCategoryModel>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.UpdateProductCategoryModel(model), Times.Once);
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
        _productViewModelServiceMock.Verify(s => s.DeleteProductCategory(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.DeleteProductCategory("c1", "p1"), Times.Once);
    }

    // --- ProductCollectionList ------------------------------------------------------------------
    // Same shape as ProductCategoryList above: HasAccess (strict) mirrors Store's CanAccessProduct and
    // Vendor's CheckAccessToProduct gating this action on both hosts.

    [TestMethod]
    public async Task ProductCollectionList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductCollectionList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.PrepareProductCollectionModel(It.IsAny<Product>()), Times.Never);
    }

    // Guards against the same regression class found in "Product categories": the denial message must be
    // templated via scope.ResourceKeyPrefix, not hardcoded to "Admin." - "Vendor.Catalog.Products.Permissions"
    // exists at the XML resource layer (en_220.xml) even though it has no Vendor call site in this region.
    [TestMethod]
    public async Task ProductCollectionList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductCollectionModel(product))
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
        _productViewModelServiceMock.Verify(s => s.InsertProductCollection(It.IsAny<ProductModel.ProductCollectionModel>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.InsertProductCollection(model), Times.Once);
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
        _productViewModelServiceMock.Verify(s => s.UpdateProductCollection(It.IsAny<ProductModel.ProductCollectionModel>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.UpdateProductCollection(model), Times.Once);
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
        _productViewModelServiceMock.Verify(s => s.DeleteProductCollection(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.DeleteProductCollection("c1", "p1"), Times.Once);
    }

    // --- RelatedProductList ------------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as ProductCategoryList/ProductCollectionList above.

    [TestMethod]
    public async Task RelatedProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.RelatedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task RelatedProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.RelatedProducts.Add(new RelatedProduct { Id = "r1", ProductId2 = "p2", DisplayOrder = 0 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.RelatedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- RelatedProductUpdate ----------------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.UpdateRelatedProductModel(It.IsAny<ProductModel.RelatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RelatedProductUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateRelatedProductModel(model), Times.Once);
    }

    // --- RelatedProductDelete ----------------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.DeleteRelatedProductModel(It.IsAny<ProductModel.RelatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RelatedProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteRelatedProductModel(model), Times.Once);
    }

    // --- RelatedProductAddPopup (GET) --------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductAddPopupGet_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareRelatedProductModel("store-1"))
            .ReturnsAsync(new ProductModel.AddRelatedProductModel());

        var result = await _controller.RelatedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddRelatedProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productViewModelServiceMock.Verify(s => s.PrepareRelatedProductModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task RelatedProductAddPopupGet_NoDefaultStoreId_PassesEmptyString()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock.Setup(s => s.PrepareRelatedProductModel(""))
            .ReturnsAsync(new ProductModel.AddRelatedProductModel());

        var result = await _controller.RelatedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareRelatedProductModel(""), Times.Once);
    }

    // --- RelatedProductAddPopupList -----------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRelatedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRelatedProductModel();
        var result = await _controller.RelatedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task RelatedProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRelatedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRelatedProductModel { SearchStoreId = "explicit" };
        var result = await _controller.RelatedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- RelatedProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original RelatedProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach related-product mappings onto another vendor's
    // product by posting its id.

    [TestMethod]
    public async Task RelatedProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddRelatedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RelatedProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.InsertRelatedProductModel(It.IsAny<ProductModel.AddRelatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RelatedProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddRelatedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RelatedProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productViewModelServiceMock.Verify(s => s.InsertRelatedProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task RelatedProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var reprepared = new ProductModel.AddRelatedProductModel();
        _productViewModelServiceMock.Setup(s => s.PrepareRelatedProductModel("store-1")).ReturnsAsync(reprepared);
        var model = new ProductModel.AddRelatedProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.RelatedProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productViewModelServiceMock.Verify(s => s.InsertRelatedProductModel(It.IsAny<ProductModel.AddRelatedProductModel>()), Times.Never);
    }

    // --- SimilarProductList ------------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList above.

    [TestMethod]
    public async Task SimilarProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.SimilarProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task SimilarProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.SimilarProducts.Add(new SimilarProduct { Id = "r1", ProductId2 = "p2", DisplayOrder = 0 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.SimilarProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- SimilarProductUpdate ----------------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.UpdateSimilarProductModel(It.IsAny<ProductModel.SimilarProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task SimilarProductUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateSimilarProductModel(model), Times.Once);
    }

    // --- SimilarProductDelete ----------------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.DeleteSimilarProductModel(It.IsAny<ProductModel.SimilarProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task SimilarProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteSimilarProductModel(model), Times.Once);
    }

    // --- SimilarProductAddPopup (GET) --------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductAddPopupGet_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareSimilarProductModel("store-1"))
            .ReturnsAsync(new ProductModel.AddSimilarProductModel());

        var result = await _controller.SimilarProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddSimilarProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productViewModelServiceMock.Verify(s => s.PrepareSimilarProductModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task SimilarProductAddPopupGet_NoDefaultStoreId_PassesEmptyString()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock.Setup(s => s.PrepareSimilarProductModel(""))
            .ReturnsAsync(new ProductModel.AddSimilarProductModel());

        var result = await _controller.SimilarProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareSimilarProductModel(""), Times.Once);
    }

    // --- SimilarProductAddPopupList -----------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddSimilarProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddSimilarProductModel();
        var result = await _controller.SimilarProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task SimilarProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddSimilarProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddSimilarProductModel { SearchStoreId = "explicit" };
        var result = await _controller.SimilarProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- SimilarProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original SimilarProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach similar-product mappings onto another vendor's
    // product by posting its id.

    [TestMethod]
    public async Task SimilarProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddSimilarProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.SimilarProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.InsertSimilarProductModel(It.IsAny<ProductModel.AddSimilarProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task SimilarProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddSimilarProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.SimilarProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productViewModelServiceMock.Verify(s => s.InsertSimilarProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task SimilarProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var reprepared = new ProductModel.AddSimilarProductModel();
        _productViewModelServiceMock.Setup(s => s.PrepareSimilarProductModel("store-1")).ReturnsAsync(reprepared);
        var model = new ProductModel.AddSimilarProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.SimilarProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productViewModelServiceMock.Verify(s => s.InsertSimilarProductModel(It.IsAny<ProductModel.AddSimilarProductModel>()), Times.Never);
    }

    // --- BundleProductList ------------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList/SimilarProductList above.

    [TestMethod]
    public async Task BundleProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

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
        _productViewModelServiceMock.Verify(s => s.UpdateBundleProductModel(It.IsAny<ProductModel.BundleProductModel>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.UpdateBundleProductModel(model), Times.Once);
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
        _productViewModelServiceMock.Verify(s => s.DeleteBundleProductModel(It.IsAny<ProductModel.BundleProductModel>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.DeleteBundleProductModel(model), Times.Once);
    }

    // --- BundleProductAddPopup (GET) --------------------------------------------------------------

    [TestMethod]
    public async Task BundleProductAddPopupGet_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareBundleProductModel("store-1"))
            .ReturnsAsync(new ProductModel.AddBundleProductModel());

        var result = await _controller.BundleProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddBundleProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productViewModelServiceMock.Verify(s => s.PrepareBundleProductModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task BundleProductAddPopupGet_NoDefaultStoreId_PassesEmptyString()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock.Setup(s => s.PrepareBundleProductModel(""))
            .ReturnsAsync(new ProductModel.AddBundleProductModel());

        var result = await _controller.BundleProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareBundleProductModel(""), Times.Once);
    }

    // --- BundleProductAddPopupList -----------------------------------------------------------------

    [TestMethod]
    public async Task BundleProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock
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
        _productViewModelServiceMock
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
        _productViewModelServiceMock.Verify(s => s.InsertBundleProductModel(It.IsAny<ProductModel.AddBundleProductModel>()), Times.Never);
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
        _productViewModelServiceMock.Verify(s => s.InsertBundleProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task BundleProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var reprepared = new ProductModel.AddBundleProductModel();
        _productViewModelServiceMock.Setup(s => s.PrepareBundleProductModel("store-1")).ReturnsAsync(reprepared);
        var model = new ProductModel.AddBundleProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.BundleProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productViewModelServiceMock.Verify(s => s.InsertBundleProductModel(It.IsAny<ProductModel.AddBundleProductModel>()), Times.Never);
    }

    // --- CrossSellProductList ---------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList/BundleProductList above. Admin's
    // original CrossSellProductList had no check at all.

    [TestMethod]
    public async Task CrossSellProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.CrossSellProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task CrossSellProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.CrossSellProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.CrossSellProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- CrossSellProductDelete -------------------------------------------------------------------
    // Admin/Store both throw ArgumentException when the product does not exist; Vendor's original
    // CrossSellProductDelete had no ownership check at all - closed here the same way as List above.

    [TestMethod]
    public async Task CrossSellProductDelete_ProductNotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.CrossSellProductDelete(model));
    }

    [TestMethod]
    public async Task CrossSellProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.CrossSellProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.CrossSellProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.DeleteCrossSellProduct(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task CrossSellProductDelete_NoMatchingCrossSellProduct_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.CrossSellProductDelete(model));
    }

    [TestMethod]
    public async Task CrossSellProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.CrossSellProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.CrossSellProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteCrossSellProduct("p1", "p2"), Times.Once);
    }

    // --- CrossSellProductAddPopup (GET) -------------------------------------------------------------

    [TestMethod]
    public async Task CrossSellProductAddPopupGet_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareCrossSellProductModel("store-1"))
            .ReturnsAsync(new ProductModel.AddCrossSellProductModel());

        var result = await _controller.CrossSellProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddCrossSellProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productViewModelServiceMock.Verify(s => s.PrepareCrossSellProductModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopupGet_NoDefaultStoreId_PassesEmptyString()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock.Setup(s => s.PrepareCrossSellProductModel(""))
            .ReturnsAsync(new ProductModel.AddCrossSellProductModel());

        var result = await _controller.CrossSellProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareCrossSellProductModel(""), Times.Once);
    }

    // --- CrossSellProductAddPopupList ---------------------------------------------------------------

    [TestMethod]
    public async Task CrossSellProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddCrossSellProductModel();
        var result = await _controller.CrossSellProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddCrossSellProductModel { SearchStoreId = "explicit" };
        var result = await _controller.CrossSellProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- CrossSellProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original CrossSellProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach cross-sell-product mappings onto another
    // vendor's product by posting its id.

    [TestMethod]
    public async Task CrossSellProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddCrossSellProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.CrossSellProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.InsertCrossSellProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddCrossSellProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.CrossSellProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productViewModelServiceMock.Verify(s => s.InsertCrossSellProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var reprepared = new ProductModel.AddCrossSellProductModel();
        _productViewModelServiceMock.Setup(s => s.PrepareCrossSellProductModel("store-1")).ReturnsAsync(reprepared);
        var model = new ProductModel.AddCrossSellProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.CrossSellProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productViewModelServiceMock.Verify(s => s.InsertCrossSellProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>()), Times.Never);
    }

    // --- RecommendedProductList ---------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as CrossSellProductList above. Admin's original
    // RecommendedProductList had no check at all. Vendor's original signature also dropped the
    // DataSourceRequest command parameter entirely - kept here for parity with Admin/Store.

    [TestMethod]
    public async Task RecommendedProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.RecommendedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task RecommendedProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.RecommendedProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.RecommendedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- RecommendedProductDelete -------------------------------------------------------------------
    // Admin/Store/Vendor all throw ArgumentException when the product does not exist; Vendor's original
    // RecommendedProductDelete had no ownership check at all - closed here the same way as List above.

    [TestMethod]
    public async Task RecommendedProductDelete_ProductNotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.RecommendedProductDelete(model));
    }

    [TestMethod]
    public async Task RecommendedProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.RecommendedProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.RecommendedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.DeleteRecommendedProduct(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RecommendedProductDelete_NoMatchingRecommendedProduct_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.RecommendedProductDelete(model));
    }

    [TestMethod]
    public async Task RecommendedProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.RecommendedProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.RecommendedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteRecommendedProduct("p1", "p2"), Times.Once);
    }

    // --- RecommendedProductAddPopup (GET) -------------------------------------------------------------

    [TestMethod]
    public async Task RecommendedProductAddPopupGet_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareRecommendedProductModel("store-1"))
            .ReturnsAsync(new ProductModel.AddRecommendedProductModel());

        var result = await _controller.RecommendedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddRecommendedProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productViewModelServiceMock.Verify(s => s.PrepareRecommendedProductModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopupGet_NoDefaultStoreId_PassesEmptyString()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock.Setup(s => s.PrepareRecommendedProductModel(""))
            .ReturnsAsync(new ProductModel.AddRecommendedProductModel());

        var result = await _controller.RecommendedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareRecommendedProductModel(""), Times.Once);
    }

    // --- RecommendedProductAddPopupList ---------------------------------------------------------------

    [TestMethod]
    public async Task RecommendedProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRecommendedProductModel();
        var result = await _controller.RecommendedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRecommendedProductModel { SearchStoreId = "explicit" };
        var result = await _controller.RecommendedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- RecommendedProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original RecommendedProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach recommended-product mappings onto another
    // vendor's product by posting its id.

    [TestMethod]
    public async Task RecommendedProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddRecommendedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RecommendedProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.InsertRecommendedProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddRecommendedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RecommendedProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productViewModelServiceMock.Verify(s => s.InsertRecommendedProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var reprepared = new ProductModel.AddRecommendedProductModel();
        _productViewModelServiceMock.Setup(s => s.PrepareRecommendedProductModel("store-1")).ReturnsAsync(reprepared);
        var model = new ProductModel.AddRecommendedProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.RecommendedProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productViewModelServiceMock.Verify(s => s.InsertRecommendedProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>()), Times.Never);
    }

    // --- AssociatedProductList -----------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList above. Admin's original had no
    // check at all.

    [TestMethod]
    public async Task AssociatedProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.AssociatedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task AssociatedProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productServiceMock.Setup(p => p.GetAssociatedProducts("p1", "", "", true))
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
    public async Task AssociatedProductAddPopupGet_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productViewModelServiceMock.Setup(s => s.PrepareAssociatedProductModel("store-1"))
            .ReturnsAsync(new ProductModel.AddAssociatedProductModel());

        var result = await _controller.AssociatedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddAssociatedProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productViewModelServiceMock.Verify(s => s.PrepareAssociatedProductModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopupGet_NoDefaultStoreId_PassesEmptyString()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productViewModelServiceMock.Setup(s => s.PrepareAssociatedProductModel(""))
            .ReturnsAsync(new ProductModel.AddAssociatedProductModel());

        var result = await _controller.AssociatedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAssociatedProductModel(""), Times.Once);
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
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var reprepared = new ProductModel.AddAssociatedProductModel();
        _productViewModelServiceMock.Setup(s => s.PrepareAssociatedProductModel("store-1")).ReturnsAsync(reprepared);
        var model = new ProductModel.AddAssociatedProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.AssociatedProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productViewModelServiceMock.Verify(s => s.InsertAssociatedProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>()), Times.Never);
    }

    // --- Product pictures ---------------------------------------------------------------------------
    // ProductPictureAdd is deliberately not covered here (same rationale as Store's original
    // ProductControllerTests): reaching its HasAccess check requires a non-empty IFormFileCollection and
    // a prior Pictures-permission check via IPermissionService, disproportionate setup for what is
    // otherwise the same one-line HasAccess condition covered everywhere else in this region.

    // --- ProductPictureList --------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPictureList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductPictureList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.PrepareProductPicturesModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPictureList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductPicturesModel(product))
            .ReturnsAsync(new List<ProductModel.ProductPictureModel> { new() { Id = "pic1" } });

        var result = await _controller.ProductPictureList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductPicturePopup (GET) -------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPicturePopupGet_ProductNotFound_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Product not exist", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPicturePopupGet_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductPicturePopupGet_ScopeGrantsAccess_PictureNotFound_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Product picture not exist", content.Content);
    }

    [TestMethod]
    public async Task ProductPicturePopupGet_ScopeGrantsAccess_PictureFound_ReturnsView()
    {
        var pp = new ProductPicture { Id = "pic1" };
        var product = new Product { Id = "p1" };
        product.ProductPictures.Add(pp);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { Id = "pic1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductPictureModel(product, pp))
            .ReturnsAsync((model, (Picture)null));

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
    }

    // --- ProductPicturePopup (POST) ------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPicturePopupPost_ProductNotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductPicturePopup(model));
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_ScopeDeniesAccess_Throws_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        // Regression guard: Vendor's original ProductPicturePopup(POST) had no access check at all,
        // letting any vendor rename/re-alt-text another vendor's product picture by posting its
        // productId/model.Id.
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductPicturePopup(model));
        _productViewModelServiceMock.Verify(s => s.UpdateProductPicture(It.IsAny<ProductModel.ProductPictureModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_ScopeGrantsAccess_PictureNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductPicturePopup(model));
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_ScopeGrantsAccess_ValidModel_Updates()
    {
        var pp = new ProductPicture { Id = "pic1" };
        var product = new Product { Id = "p1" };
        product.ProductPictures.Add(pp);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        var result = await _controller.ProductPicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productViewModelServiceMock.Verify(s => s.UpdateProductPicture(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_InvalidModelState_ReturnsView()
    {
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.ProductPicturePopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // --- ProductPictureDelete -------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPictureDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        // Regression guard: Vendor's original ProductPictureDelete had no access check at all, letting
        // any vendor delete another vendor's product picture by posting its productId/model.Id.
        var result = await _controller.ProductPictureDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.DeleteProductPicture(It.IsAny<ProductModel.ProductPictureModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPictureDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        var result = await _controller.ProductPictureDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteProductPicture(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductPictureDelete_ScopeGrantsAccess_InvalidModelState_ReturnsKendoGridError_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.ProductPictureDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteProductPicture(It.IsAny<ProductModel.ProductPictureModel>()), Times.Never);
    }

    // --- Product specification attributes -----------------------------------------------------------
    // GetOptionsByAttributeId is deliberately not covered here (same rationale as Product pictures'
    // ProductPictureAdd note): identical, unscoped across all three hosts, no access check involved.

    // --- ProductSpecAttrList --------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductSpecAttrList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductSpecificationAttributeModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductSpecificationAttributeModel(product))
            .ReturnsAsync(new List<ProductSpecificationAttributeModel> { new() { Id = "psa1" } });

        var result = await _controller.ProductSpecAttrList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        var json = (JsonResult)result;
        var grid = (Grand.Web.Common.DataSource.DataSourceResult)json.Value;
        Assert.AreEqual(1, grid.Total);
    }

    // --- ProductSpecAttrPopup (GET) -----------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrPopupGet_ScopeDeniesAccess_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, "p1", "psa1");

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductSpecificationAttributeModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupGet_ScopeGrantsAccess_NewAttribute_ReturnsView()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var specAttrServiceMock = new Mock<ISpecificationAttributeService>();
        specAttrServiceMock.Setup(s => s.GetSpecificationAttributes(It.IsAny<string>(), 0, int.MaxValue))
            .ReturnsAsync(new PagedList<SpecificationAttribute>(new List<SpecificationAttribute>(), 0, int.MaxValue));

        var result = await _controller.ProductSpecAttrPopup(specAttrServiceMock.Object, "p1", "");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as ProductModel.AddProductSpecificationAttributeModel;
        Assert.IsNotNull(model);
        Assert.IsTrue(model.ShowOnProductPage);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupGet_ScopeGrantsAccess_ExistingAttribute_ReturnsPopulatedView()
    {
        var psa = new ProductSpecificationAttribute { Id = "psa1", SpecificationAttributeOptionId = "opt1" };
        var product = new Product { Id = "p1" };
        product.ProductSpecificationAttributes.Add(psa);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var specAttrServiceMock = new Mock<ISpecificationAttributeService>();
        specAttrServiceMock.Setup(s => s.GetSpecificationAttributes(It.IsAny<string>(), 0, int.MaxValue))
            .ReturnsAsync(new PagedList<SpecificationAttribute>(new List<SpecificationAttribute>(), 0, int.MaxValue));

        var result = await _controller.ProductSpecAttrPopup(specAttrServiceMock.Object, "p1", "psa1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as ProductModel.AddProductSpecificationAttributeModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("psa1", model.Id);
    }

    // --- ProductSpecAttrPopup (POST) ----------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ProductNotFound_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductSpecificationAttributeModel(It.IsAny<Product>(), It.IsAny<ProductSpecificationAttribute>(),
                It.IsAny<ProductModel.AddProductSpecificationAttributeModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ScopeDeniesAccess_ReturnsContent_DoesNotInsertOrUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        // Regression guard: Vendor's original ProductSpecAttrPopup(POST) had no access check at all,
        // letting any vendor add/edit specification attributes on another vendor's product by posting
        // its id.
        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductSpecificationAttributeModel(It.IsAny<ProductModel.AddProductSpecificationAttributeModel>(),
                It.IsAny<Product>()), Times.Never);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductSpecificationAttributeModel(It.IsAny<Product>(), It.IsAny<ProductSpecificationAttribute>(),
                It.IsAny<ProductModel.AddProductSpecificationAttributeModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ScopeGrantsAccess_NewAttribute_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa-new" };

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.InsertProductSpecificationAttributeModel(model, product), Times.Once);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ScopeGrantsAccess_ExistingAttribute_Updates()
    {
        var psa = new ProductSpecificationAttribute { Id = "psa1" };
        var product = new Product { Id = "p1" };
        product.ProductSpecificationAttributes.Add(psa);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductSpecificationAttributeModel(product, psa, model), Times.Once);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_InvalidModelState_ReturnsView_DoesNotAccessProduct()
    {
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };
        _controller.ModelState.AddModelError("x", "error");
        var specAttrServiceMock = new Mock<ISpecificationAttributeService>();
        specAttrServiceMock.Setup(s => s.GetSpecificationAttributes(It.IsAny<string>(), 0, int.MaxValue))
            .ReturnsAsync(new PagedList<SpecificationAttribute>(new List<SpecificationAttribute>(), 0, int.MaxValue));

        var result = await _controller.ProductSpecAttrPopup(specAttrServiceMock.Object, model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // --- ProductSpecAttrDelete ------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrDelete_ProductNotFound_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductSpecificationAttribute(It.IsAny<Product>(), It.IsAny<ProductSpecificationAttribute>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_ScopeDeniesAccess_ReturnsContent_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        // Regression guard: Vendor's original ProductSpecAttrDelete had no access check at all, letting
        // any vendor delete another vendor's specification attribute mapping by posting its
        // productId/model.Id.
        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductSpecificationAttribute(It.IsAny<Product>(), It.IsAny<ProductSpecificationAttribute>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var psa = new ProductSpecificationAttribute { Id = "psa1" };
        var product = new Product { Id = "p1" };
        product.ProductSpecificationAttributes.Add(psa);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteProductSpecificationAttribute(product, psa), Times.Once);
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_ScopeGrantsAccess_AttributeNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductSpecAttrDelete(model));
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_InvalidModelState_ReturnsKendoGridError_DoesNotAccessProduct()
    {
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
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
        // No scope filtering here: productViewModelService.PrepareProducts is host-specific and already
        // returns only the caller's products (Vendor's implementation constrains by CurrentVendor.Id
        // internally) - the controller trusts it, same as ProductList trusts PrepareProductsModel.
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
    public async Task ExportExcelAll_ExportThrows_ReturnsRedirectToList()
    {
        var model = new ProductListModel();
        _productViewModelServiceMock.Setup(s => s.PrepareProducts(model)).ReturnsAsync([]);
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock.Setup(e => e.Export(It.IsAny<IEnumerable<Product>>()))
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
        exportManagerMock.Setup(e => e.Export(It.Is<IEnumerable<Product>>(p => !p.Any())))
            .ReturnsAsync([9]);

        var result = await _controller.ExportExcelSelected(null, exportManagerMock.Object);

        var file = result as FileContentResult;
        Assert.IsNotNull(file);
        _productServiceMock.Verify(p => p.GetProductsByIds(It.IsAny<string[]>(), true), Times.Never);
    }

    [TestMethod]
    public async Task ExportExcelSelected_FiltersOutProductsScopeDenies()
    {
        // Mirrors Vendor's original explicit HasAccessToProduct re-check on selectedIds (caller-supplied,
        // not derived from a scoped search, unlike ExportExcelAll) - applied unconditionally here so
        // Admin (where the original had no check at all) gets the same protection.
        var owned = new Product { Id = "owned" };
        var foreign = new Product { Id = "foreign" };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" }, true))
            .ReturnsAsync([owned, foreign]);
        _scopeMock.Setup(s => s.HasAccess(owned)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);
        var exportManagerMock = new Mock<IExportManager<Product>>();
        exportManagerMock
            .Setup(e => e.Export(It.Is<IEnumerable<Product>>(p => p.Single() == owned)))
            .ReturnsAsync([7]);

        var result = await _controller.ExportExcelSelected("owned,foreign", exportManagerMock.Object);

        var file = result as FileContentResult;
        Assert.IsNotNull(file);
        exportManagerMock.Verify(e => e.Export(It.Is<IEnumerable<Product>>(p => p.Single() == owned)), Times.Once);
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
        importManagerMock.Verify(i => i.Import(It.IsAny<Stream>()), Times.Never);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Common.UploadFile"), Times.Once);
    }

    [TestMethod]
    public async Task ImportExcel_NullFile_DoesNotImport_RedirectsToListWithError()
    {
        var importManagerMock = new Mock<IImportManager<ProductDto>>();

        var result = await _controller.ImportExcel(null, importManagerMock.Object);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        importManagerMock.Verify(i => i.Import(It.IsAny<Stream>()), Times.Never);
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
    public async Task BulkEdit_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");
        _productViewModelServiceMock.Setup(s => s.PrepareBulkEditListModel("store1"))
            .ReturnsAsync(new BulkEditListModel());

        var result = await _controller.BulkEdit();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        _productViewModelServiceMock.Verify(s => s.PrepareBulkEditListModel("store1"), Times.Once);
    }

    [TestMethod]
    public async Task BulkEdit_NullDefaultStoreId_PassesEmptyString()
    {
        // Admin/Vendor: scope.DefaultStoreId is null (Admin is global; Vendor is not store-scoped) -
        // matches Admin's original parameterless call and Vendor's own service's parameterless method.
        _productViewModelServiceMock.Setup(s => s.PrepareBulkEditListModel(""))
            .ReturnsAsync(new BulkEditListModel());

        await _controller.BulkEdit();

        _productViewModelServiceMock.Verify(s => s.PrepareBulkEditListModel(""), Times.Once);
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
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" }, true))
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
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "foreign" }, true))
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
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" }, true))
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
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "foreign" }, true))
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
        _productServiceMock.Verify(p => p.GetProductsByIds(It.IsAny<string[]>(), It.IsAny<bool>()), Times.Never);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.IsAny<Product>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceList_ScopeGrantsAccess_UsesScopeDefaultStoreId_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");
        var tierPrices = new List<ProductModel.TierPriceModel> { new() { Id = "tp1" } };
        _productViewModelServiceMock.Setup(s => s.PrepareTierPriceModel(product, "store1")).ReturnsAsync(tierPrices);

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    [TestMethod]
    public async Task TierPriceList_ScopeGrantsAccess_NullDefaultStoreId_PassesEmptyString()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareTierPriceModel(product, ""))
            .ReturnsAsync(new List<ProductModel.TierPriceModel>());

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareTierPriceModel(product, ""), Times.Once);
    }

    // --- TierPriceCreatePopup (GET) ----------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceCreatePopup_Get_PreparesModelWithScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");

        var result = await _controller.TierPriceCreatePopup("p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.Is<ProductModel.TierPriceModel>(m => m.ProductId == "p1"), "store1"),
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
            s => s.PrepareTierPriceModel(It.IsAny<ProductModel.TierPriceModel>(), It.IsAny<string>()), Times.Never);
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
    public async Task TierPriceEditPopup_Get_ScopeGrantsAccess_PreparesModelWithScopeDefaultStoreId()
    {
        var product = new Product { Id = "p1" };
        product.TierPrices.Add(new TierPrice { Id = "tp1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");

        var result = await _controller.TierPriceEditPopup("tp1", "p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.Is<ProductModel.TierPriceModel>(m => m.ProductId == "p1"), "store1"),
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
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
    public async Task AssociateProductToAttributeValuePopup_Get_PassesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");
        _productViewModelServiceMock.Setup(s => s.PrepareAssociateProductToAttributeValueModel("store1"))
            .ReturnsAsync(new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel());

        var result = await _controller.AssociateProductToAttributeValuePopup();

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAssociateProductToAttributeValueModel("store1"),
            Times.Once);
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Get_NullDefaultStoreId_PassesEmptyString()
    {
        _productViewModelServiceMock.Setup(s => s.PrepareAssociateProductToAttributeValueModel(""))
            .ReturnsAsync(new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel());

        var result = await _controller.AssociateProductToAttributeValuePopup();

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAssociateProductToAttributeValueModel(""), Times.Once);
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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

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
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
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
}
