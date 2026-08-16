using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<ProductProfile>(); });
        AutoMapperConfig.Init(mapperConfig);

        _productServiceMock = new Mock<IProductService>();
        _productViewModelServiceMock = new Mock<IProductViewModelService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Admin");
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

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
            new Mock<IPermissionService>().Object,
            new Mock<IEnumTranslationService>().Object,
            _scopeMock.Object);

        var httpContext = new DefaultHttpContext();
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
}
