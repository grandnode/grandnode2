using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
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
    private Mock<IProductService> _productServiceMock;
    private Mock<IProductViewModelService> _productViewModelServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
        _productViewModelServiceMock = new Mock<IProductViewModelService>();
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
            new Mock<IPermissionService>().Object,
            new Mock<IEnumTranslationService>().Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
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
}
