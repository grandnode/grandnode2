using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

// Characterization test for ProductController.EditWarningCheck (ARCH-001 Phase 1 Task 11). This is the
// one piece of behavior newly hand-written for this task (re-derived from the original pre-migration
// Edit(GET) action's if/else, not copied) - everything else this controller does lives in, and is
// covered by, BaseProductControllerTests.cs (see that file's own header comment on the Task 13
// consolidation this replaces). The condition is unusual (warns when NOT limited to stores at all, or
// when limited AND the staff member's store is one of several) and easy to get backwards, per the
// plan's own warning - this test exists so a future regression here fails loudly instead of silently.
[TestClass]
public class ProductControllerTests
{
    private const string StaffStoreId = "store-1";
    private const string OtherStoreId = "store-2";

    private ProductController _controller;
    private Mock<IProductService> _productServiceMock;
    private Mock<IAdminDataScope<Product>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
        var productViewModelServiceMock = new Mock<IProductViewModelService>();
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns(StaffStoreId);
        _scopeMock.Setup(s => s.CanView(It.IsAny<Product>())).ReturnsAsync(true);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
            .ReturnsAsync(new List<Domain.Localization.Language>());

        _controller = new ProductController(
            productViewModelServiceMock.Object,
            _productServiceMock.Object,
            new Mock<IInventoryManageService>().Object,
            languageServiceMock.Object,
            translationServiceMock.Object,
            new Mock<IProductReservationService>().Object,
            new Mock<IAuctionService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IPermissionService>().Object,
            new Mock<IEnumTranslationService>().Object,
            _scopeMock.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private bool WarningWasRaised()
        => _controller.TempData["grand.notifications.Warning"] is List<string> { Count: > 0 };

    [TestMethod]
    public async Task EditGet_ProductNotLimitedToAnyStore_RaisesWarning()
    {
        var product = new Product { Id = "p1", LimitedToStores = false };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        await _controller.Edit("p1");

        Assert.IsTrue(WarningWasRaised(), "A product visible to every store must warn a store-scoped editor.");
    }

    [TestMethod]
    public async Task EditGet_ProductLimitedToStaffStoreAndAnotherStore_RaisesWarning()
    {
        var product = new Product
            { Id = "p1", LimitedToStores = true, Stores = [StaffStoreId, OtherStoreId] };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        await _controller.Edit("p1");

        Assert.IsTrue(WarningWasRaised(),
            "A product shared with another store beyond the staff member's own must still warn.");
    }

    [TestMethod]
    public async Task EditGet_ProductLimitedToStaffStoreOnly_NoWarning()
    {
        var product = new Product { Id = "p1", LimitedToStores = true, Stores = [StaffStoreId] };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        await _controller.Edit("p1");

        Assert.IsFalse(WarningWasRaised(),
            "A product exclusive to the staff member's own store needs no cross-store warning.");
    }
}
