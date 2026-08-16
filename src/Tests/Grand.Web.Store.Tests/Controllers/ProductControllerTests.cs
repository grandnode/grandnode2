using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

// Characterization tests for the store-scoping checks in ProductController, ahead of the planned
// consolidation of the near-duplicate ProductController copies in Grand.Web.Admin / Grand.Web.Store /
// Grand.Web.Vendor. Note the current redirect target on denial differs from Vendor's equivalent
// (Edit id=<id> here vs List there) - that asymmetry must survive any refactor, or be called out as an
// intentional behavior change.
[TestClass]
public class ProductControllerTests
{
    private const string StaffStoreId = "store-1";
    private const string OtherStoreId = "store-2";

    private ProductController _controller;
    private Mock<IPermissionService> _permissionServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IProductViewModelService> _productViewModelServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
        _productViewModelServiceMock = new Mock<IProductViewModelService>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Language>());

        _controller = new ProductController(
            _productViewModelServiceMock.Object,
            _productServiceMock.Object,
            new Mock<IInventoryManageService>().Object,
            contextAccessorMock.Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IProductReservationService>().Object,
            new Mock<IAuctionService>().Object,
            new Mock<IDateTimeService>().Object,
            _permissionServiceMock.Object,
            new Mock<IEnumTranslationService>().Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    // --- Shared helpers for the CanAccessProduct denial tests below -------------------------------
    // Every action denies access via the same rule (AclMappingExtension.AccessToEntityByStore: an
    // explicit single foreign store beats the staff member's store). Centralizing the "denied product"
    // shape and the per-response-type assertions keeps each of the ~60 call sites below to a few lines,
    // matching the mechanical nature of the CanAccessProduct extraction itself.

    private static Product ForeignProduct(string id = "denied")
    {
        var product = new Product { Id = id, LimitedToStores = true };
        product.Stores.Add(OtherStoreId);
        return product;
    }

    private void MockAnyProductLookupAsForeign()
    {
        _productServiceMock.Setup(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(ForeignProduct());
    }

    private void MockSkuLookupAsForeign()
    {
        _productServiceMock.Setup(p => p.GetProductBySku(It.IsAny<string>())).ReturnsAsync(ForeignProduct());
    }

    private static void AssertKendoGridPermissionError(IActionResult result)
    {
        var json = result as JsonResult;
        Assert.IsNotNull(json, "expected a JsonResult");
        var data = json.Value as DataSourceResult;
        Assert.IsNotNull(data, "expected a DataSourceResult");
        Assert.AreEqual("resource", data.Errors);
    }

    private static void AssertContentPermissionError(IActionResult result)
    {
        var content = result as ContentResult;
        Assert.IsNotNull(content, "expected a ContentResult");
        Assert.AreEqual("resource", content.Content);
    }

    private static void AssertJsonErrorsPermissionError(IActionResult result)
    {
        var json = result as JsonResult;
        Assert.IsNotNull(json, "expected a JsonResult");
        var errorsProp = json.Value?.GetType().GetProperty("errors");
        Assert.IsNotNull(errorsProp, "expected an anonymous object with an 'errors' property");
        Assert.AreEqual("resource", errorsProp.GetValue(json.Value));
    }

    private static void AssertRedirectToProductList(IActionResult result)
    {
        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect, "expected a RedirectToActionResult");
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Product", redirect.ControllerName);
    }

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
    public async Task Delete_ProductOutsideStaffStore_RedirectsToEditWithoutDeleting()
    {
        var product = new Product { Id = "p1", LimitedToStores = true };
        product.Stores.Add(OtherStoreId);
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Delete("p1");

        // Unlike Vendor, denial here redirects back to Edit rather than List.
        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p1", redirect.RouteValues["id"]);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ProductInStaffStore_DeletesAndRedirectsToList()
    {
        var product = new Product { Id = "p1", LimitedToStores = true };
        product.Stores.Add(StaffStoreId);
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Once);
    }

    [TestMethod]
    public async Task Delete_ProductNotLimitedToAnyStore_IsDenied()
    {
        // Counter-intuitive but current behavior: AccessToEntityByStore only grants access when
        // LimitedToStores is true AND the product belongs to exactly one store (this one). A
        // "global" (LimitedToStores=false) product is therefore NOT deletable by store staff -
        // see AclMappingExtension.AccessToEntityByStore. A refactor must not silently "fix" this.
        var product = new Product { Id = "p1", LimitedToStores = false };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ProductInMultipleStoresIncludingStaffStore_IsDenied()
    {
        // Same source: Stores.Count == 1 is required, so a product shared across stores is denied
        // even to a staff member of one of those stores.
        var product = new Product { Id = "p1", LimitedToStores = true };
        product.Stores.Add(StaffStoreId);
        product.Stores.Add(OtherStoreId);
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Never);
    }

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
    public async Task EditPost_ProductOutsideStaffStore_RedirectsToEditWithoutUpdating()
    {
        // Same check, same "Edit" redirect target as Delete - but note this is a *different* check
        // from Edit(GET), which additionally allows a multi-store product through with a warning.
        // Do not fold this into a helper shared with Edit(GET).
        var product = new Product { Id = "p1", LimitedToStores = true };
        product.Stores.Add(OtherStoreId);
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Edit(new ProductModel { Id = "p1" }, continueEditing: false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p1", redirect.RouteValues["id"]);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductModel(It.IsAny<Product>(), It.IsAny<ProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ProductSharedAcrossMultipleStoresIncludingStaffStore_ShowsFormWithWarning()
    {
        // Edit(GET)'s permissive branch: a product limited to more than one store, one of which is
        // this staff member's store, is NOT denied here - it is shown with a warning instead. This is
        // the one path that must stay outside any shared "authorize or redirect" helper.
        var product = new Product { Id = "p1", LimitedToStores = true };
        product.Stores.Add(StaffStoreId);
        product.Stores.Add(OtherStoreId);
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Edit("p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductModel(It.IsAny<ProductModel>(), product, false, false), Times.Once);
    }

    [TestMethod]
    public async Task EditGet_ProductInSingleOtherStore_RedirectsToList()
    {
        // The strict branch of Edit(GET) - a product limited to exactly one store that isn't this
        // staff member's - as opposed to the permissive multi-store branch tested above.
        var product = new Product { Id = "p1", LimitedToStores = true };
        product.Stores.Add(OtherStoreId);
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Edit("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductModel(It.IsAny<ProductModel>(), It.IsAny<Product>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task GoToSku_ProductNotAccessible_RedirectsToEditWithoutExposingIt()
    {
        MockSkuLookupAsForeign();

        var result = await _controller.GoToSku(new ProductListModel { GoDirectlyToSku = "sku1" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("denied", redirect.RouteValues["id"]);
    }

    [TestMethod]
    public async Task LoadProductFriendlyNames_SkipsNamesOfProductsNotAccessible()
    {
        var owned = new Product { Id = "owned", Name = "Owned", LimitedToStores = true };
        owned.Stores.Add(StaffStoreId);
        var foreign = ForeignProduct("foreign");
        foreign.Name = "Foreign";
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "owned", "foreign" }, true))
            .ReturnsAsync(new List<Product> { owned, foreign });

        var result = await _controller.LoadProductFriendlyNames("owned,foreign");

        // Note: the trailing ", " is current behavior, not intentional - the separator is appended
        // based on loop position ("not the last id"), not on whether a name was actually appended for
        // the *previous* id. Preserved as-is; not this refactor's concern to fix.
        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var text = json.Value.GetType().GetProperty("Text")?.GetValue(json.Value) as string;
        Assert.AreEqual("Owned, ", text);
    }

    // --- Product categories ---------------------------------------------------------------------

    [TestMethod]
    public async Task ProductCategoryList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCategoryList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductCategoryInsert_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCategoryInsert(new ProductModel.ProductCategoryModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductCategoryModel(It.IsAny<ProductModel.ProductCategoryModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCategoryUpdate_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCategoryUpdate(new ProductModel.ProductCategoryModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductCategoryModel(It.IsAny<ProductModel.ProductCategoryModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductCategoryDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCategoryDelete(new ProductModel.ProductCategoryModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductCategory(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- Product collections ----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductCollectionList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCollectionList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductCollectionInsert_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCollectionInsert(new ProductModel.ProductCollectionModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductCollectionUpdate_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCollectionUpdate(new ProductModel.ProductCollectionModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductCollectionDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductCollectionDelete(new ProductModel.ProductCollectionModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    // --- Related products --------------------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.RelatedProductList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task RelatedProductUpdate_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.RelatedProductUpdate(new ProductModel.RelatedProductModel { ProductId1 = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task RelatedProductDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.RelatedProductDelete(new ProductModel.RelatedProductModel { ProductId1 = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task RelatedProductAddPopup_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.RelatedProductAddPopup(new ProductModel.AddRelatedProductModel { ProductId = "p1" });

        AssertContentPermissionError(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertRelatedProductModel(It.IsAny<ProductModel.AddRelatedProductModel>()), Times.Never);
    }

    // --- Similar products ---------------------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.SimilarProductList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task SimilarProductUpdate_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.SimilarProductUpdate(new ProductModel.SimilarProductModel { ProductId1 = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task SimilarProductDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.SimilarProductDelete(new ProductModel.SimilarProductModel { ProductId1 = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task SimilarProductAddPopup_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.SimilarProductAddPopup(new ProductModel.AddSimilarProductModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    // --- Bundle products ------------------------------------------------------------------------------

    [TestMethod]
    public async Task BundleProductList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.BundleProductList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task BundleProductUpdate_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.BundleProductUpdate(new ProductModel.BundleProductModel { ProductBundleId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task BundleProductDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.BundleProductDelete(new ProductModel.BundleProductModel { ProductBundleId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task BundleProductAddPopup_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.BundleProductAddPopup(new ProductModel.AddBundleProductModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    // --- Cross-sell products --------------------------------------------------------------------------

    [TestMethod]
    public async Task CrossSellProductList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.CrossSellProductList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task CrossSellProductDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.CrossSellProductDelete(new ProductModel.CrossSellProductModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopup_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.CrossSellProductAddPopup(new ProductModel.AddCrossSellProductModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    // --- Recommended products -------------------------------------------------------------------------

    [TestMethod]
    public async Task RecommendedProductList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.RecommendedProductList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task RecommendedProductDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.RecommendedProductDelete(new ProductModel.RecommendedProductModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopup_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.RecommendedProductAddPopup(new ProductModel.AddRecommendedProductModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    // --- Associated products ----------------------------------------------------------------------------

    [TestMethod]
    public async Task AssociatedProductList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.AssociatedProductList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task AssociatedProductUpdate_AssociatedProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.AssociatedProductUpdate(new ProductModel.AssociatedProductModel { Id = "p1" });

        AssertKendoGridPermissionError(result);
        _productServiceMock.Verify(s => s.UpdateAssociatedProduct(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociatedProductDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.AssociatedProductDelete(new ProductModel.AssociatedProductModel { Id = "p1" });

        AssertKendoGridPermissionError(result);
        _productViewModelServiceMock.Verify(s => s.DeleteAssociatedProduct(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopup_ParentProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.AssociatedProductAddPopup(new ProductModel.AddAssociatedProductModel {
            ProductId = "p1"
        });

        AssertContentPermissionError(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertAssociatedProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociatedProductAddPopup_ParentOwnedButCandidateNotAccessible_ExcludesCandidate()
    {
        // The parent product is the vendor's own, but one selected candidate belongs to another
        // store - AssociatedProductAddPopup filters SelectedProductIds down to only the accessible
        // ones (the positive `CanAccessProduct(selected)` form) before calling InsertAssociatedProductModel.
        var parent = new Product { Id = "parent", LimitedToStores = true };
        parent.Stores.Add(StaffStoreId);
        _productServiceMock.Setup(p => p.GetProductById("parent", It.IsAny<bool>())).ReturnsAsync(parent);
        _productServiceMock.Setup(p => p.GetProductById("foreign", It.IsAny<bool>())).ReturnsAsync(ForeignProduct("foreign"));

        var model = new ProductModel.AddAssociatedProductModel {
            ProductId = "parent",
            SelectedProductIds = ["foreign"]
        };

        var result = await _controller.AssociatedProductAddPopup(model);

        AssertSuccessContent(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertAssociatedProductModel(It.IsAny<ProductModel.AddAssociatedProductModel>()), Times.Never);
    }

    private static void AssertSuccessContent(IActionResult result)
    {
        var content = result as ContentResult;
        Assert.IsNotNull(content, "expected a ContentResult (success path, not the permission-denied one)");
        Assert.AreEqual("", content.Content);
    }

    // --- Product pictures --------------------------------------------------------------------------
    // ProductPictureAdd is deliberately not covered here: reaching its CanAccessProduct check requires
    // a non-empty IFormFileCollection and a prior Pictures-permission check, disproportionate setup for
    // what is otherwise the same one-line condition covered everywhere else in this file.

    [TestMethod]
    public async Task ProductPictureList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductPictureList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductPicturePopupGet_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_ProductNotAccessible_Throws()
    {
        // Unlike its GET counterpart, the POST handler throws instead of returning an error response.
        MockAnyProductLookupAsForeign();

        try
        {
            await _controller.ProductPicturePopup(new ProductModel.ProductPictureModel { ProductId = "p1" });
            Assert.Fail("expected an ArgumentException");
        }
        catch (ArgumentException)
        {
            // expected
        }
    }

    [TestMethod]
    public async Task ProductPictureDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductPictureDelete(new ProductModel.ProductPictureModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    // --- Product specification attributes ---------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductSpecAttrList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupGet_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductSpecAttrPopup(
            new Mock<ISpecificationAttributeService>().Object, "p1", null);

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductSpecAttrPopup(
            new Mock<ISpecificationAttributeService>().Object,
            new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductSpecAttrDelete(new ProductSpecificationAttributeModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    // --- Purchased with orders / Reviews ------------------------------------------------------------

    [TestMethod]
    public async Task PurchasedWithOrders_ProductNotAccessible_ReturnsKendoGridError()
    {
        _permissionServiceMock.Setup(p => p.Authorize(It.IsAny<Permission>())).ReturnsAsync(true);
        MockAnyProductLookupAsForeign();

        var result = await _controller.PurchasedWithOrders(new DataSourceRequest(), "p1",
            new Mock<IOrderViewModelService>().Object);

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task Reviews_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.Reviews(new DataSourceRequest(), "p1", new Mock<IProductReviewService>().Object);

        AssertKendoGridPermissionError(result);
    }

    // --- Bulk editing --------------------------------------------------------------------------------

    [TestMethod]
    public async Task BulkEditDelete_FiltersOutProductsNotAccessible()
    {
        var owned = new Product { Id = "owned", LimitedToStores = true };
        owned.Stores.Add(StaffStoreId);
        _productServiceMock.Setup(p => p.GetProductById("owned", It.IsAny<bool>())).ReturnsAsync(owned);
        _productServiceMock.Setup(p => p.GetProductById("foreign", It.IsAny<bool>())).ReturnsAsync(ForeignProduct("foreign"));

        var models = new List<BulkEditProductModel> {
            new() { Id = "owned" },
            new() { Id = "foreign" }
        };

        await _controller.BulkEditDelete(models);

        _productViewModelServiceMock.Verify(s => s.DeleteBulkEdit(
            It.Is<List<BulkEditProductModel>>(list => list.Count == 1 && list[0].Id == "owned")), Times.Once);
    }

    [TestMethod]
    public async Task BulkEditUpdate_FiltersOutProductsNotAccessible()
    {
        var owned = new Product { Id = "owned", LimitedToStores = true };
        owned.Stores.Add(StaffStoreId);
        _productServiceMock.Setup(p => p.GetProductById("owned", It.IsAny<bool>())).ReturnsAsync(owned);
        _productServiceMock.Setup(p => p.GetProductById("foreign", It.IsAny<bool>())).ReturnsAsync(ForeignProduct("foreign"));

        var models = new List<BulkEditProductModel> {
            new() { Id = "owned" },
            new() { Id = "foreign" }
        };

        await _controller.BulkEditUpdate(models);

        _productViewModelServiceMock.Verify(s => s.UpdateBulkEdit(
            It.Is<IEnumerable<BulkEditProductModel>>(list => list.Count() == 1 && list.First().Id == "owned")),
            Times.Once);
    }

    // --- Product currency price ------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPriceList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductPriceList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductPriceInsert_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductPriceInsert(new ProductModel.ProductPriceModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductPriceUpdate_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductPriceUpdate(new ProductModel.ProductPriceModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductPriceDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductPriceDelete(new ProductModel.ProductPriceModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    // --- Tier prices -----------------------------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.TierPriceList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task TierPriceCreatePopup_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.TierPriceCreatePopup(new ProductModel.TierPriceModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.TierPriceEditPopup("p1", new ProductModel.TierPriceModel());

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task TierPriceDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.TierPriceDelete(new ProductModel.TierPriceDeleteModel("t1", "p1"));

        AssertKendoGridPermissionError(result);
    }

    // --- Product attributes -----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeMappingList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeMappingList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopupGet_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeMappingPopup("p1", null);

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopupPost_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeMappingPopup(
            new ProductModel.ProductAttributeMappingModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeMappingDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        var foreign = ForeignProduct();
        foreign.ProductAttributeMappings.Add(new ProductAttributeMapping { Id = "pam1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(foreign);

        var result = await _controller.ProductAttributeMappingDelete("pam1", "p1",
            new Mock<IProductAttributeService>().Object);

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeValidationRulesPopup("id1", "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopupGet_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeConditionPopup("p1", "pam1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopupPost_ProductNotAccessible_ReturnsContentError()
    {
        var foreign = ForeignProduct();
        foreign.ProductAttributeMappings.Add(new ProductAttributeMapping { Id = "pam1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(foreign);

        var result = await _controller.ProductAttributeConditionPopup(
            new ProductAttributeConditionModel { ProductId = "p1", ProductAttributeMappingId = "pam1" });

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task EditAttributeValues_ProductNotAccessible_ReturnsContentError()
    {
        var foreign = ForeignProduct();
        foreign.ProductAttributeMappings.Add(new ProductAttributeMapping { Id = "pam1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(foreign);

        var result = await _controller.EditAttributeValues("pam1", "p1", new Mock<IProductAttributeService>().Object);

        AssertContentPermissionError(result);
    }

    // --- Product attribute values ------------------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValueList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeValueList("pam1", "p1", new DataSourceRequest());

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopupGet_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeValueCreatePopup("pam1", "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopupPost_ProductNotAccessible_RedirectsToProductList()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeValueCreatePopup(
            new ProductModel.ProductAttributeValueModel { ProductId = "p1" });

        AssertRedirectToProductList(result);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopupGet_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeValueEditPopup("val1", "p1", "pam1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopupPost_ProductNotAccessible_RedirectsToProductList()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeValueEditPopup("p1",
            new ProductModel.ProductAttributeValueModel());

        AssertRedirectToProductList(result);
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ProductNotAccessible_Throws()
    {
        var foreign = ForeignProduct();
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(new ProductAttributeValue { Id = "val1" });
        foreign.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(foreign);

        try
        {
            await _controller.ProductAttributeValueDelete("val1", "pam1", "p1",
                new Mock<IProductAttributeService>().Object);
            Assert.Fail("expected an ArgumentException");
        }
        catch (ArgumentException)
        {
            // expected
        }
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_AssociatedProductNotAccessible_Throws()
    {
        MockAnyProductLookupAsForeign();

        try
        {
            await _controller.AssociateProductToAttributeValuePopup(
                new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel {
                    AssociatedToProductId = "p1"
                });
            Assert.Fail("expected an ArgumentException");
        }
        catch (ArgumentException)
        {
            // expected
        }
    }

    // --- Product attribute combinations ---------------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeCombinationList(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        var foreign = ForeignProduct();
        foreign.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(foreign);

        var result = await _controller.ProductAttributeCombinationDelete("c1", "p1",
            new Mock<IProductAttributeService>().Object);

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupGet_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.AttributeCombinationPopup("p1", "c1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupPost_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.AttributeCombinationPopup("p1", new ProductAttributeCombinationModel { ProductId = "p1" });

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task GenerateAllAttributeCombinations_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.GenerateAllAttributeCombinations("p1");

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ClearAllAttributeCombinations_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ClearAllAttributeCombinations("p1");

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceList_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeCombinationTierPriceList(new DataSourceRequest(), "p1", "c1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeCombinationTierPriceInsert("p1", "c1",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceUpdate_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeCombinationTierPriceUpdate("p1", "c1",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        AssertContentPermissionError(result);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_ProductNotAccessible_ReturnsContentError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductAttributeCombinationTierPriceDelete("p1", "c1", "t1");

        AssertContentPermissionError(result);
    }

    // --- Reservation ----------------------------------------------------------------------------------

    [TestMethod]
    public async Task ListReservations_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ListReservations(new DataSourceRequest(), "p1");

        AssertKendoGridPermissionError(result);
    }

    [TestMethod]
    public async Task GenerateCalendar_ProductNotAccessible_ReturnsJsonErrors()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.GenerateCalendar("p1", new ProductModel.GenerateCalendarModel());

        AssertJsonErrorsPermissionError(result);
    }

    [TestMethod]
    public async Task ClearCalendar_ProductNotAccessible_ReturnsJsonErrors()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ClearCalendar("p1");

        AssertJsonErrorsPermissionError(result);
    }

    [TestMethod]
    public async Task ClearOld_ProductNotAccessible_ReturnsJsonErrors()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ClearOld("p1");

        AssertJsonErrorsPermissionError(result);
    }

    [TestMethod]
    public async Task ProductReservationDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ProductReservationDelete(new ProductModel.ReservationModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }

    // --- Bids -----------------------------------------------------------------------------------------

    [TestMethod]
    public async Task ListBids_ProductNotAccessible_ReturnsJsonErrors()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.ListBids(new DataSourceRequest(), "p1");

        AssertJsonErrorsPermissionError(result);
    }

    [TestMethod]
    public async Task BidDelete_ProductNotAccessible_ReturnsKendoGridError()
    {
        MockAnyProductLookupAsForeign();

        var result = await _controller.BidDelete(new ProductModel.BidModel { ProductId = "p1" });

        AssertKendoGridPermissionError(result);
    }
}
