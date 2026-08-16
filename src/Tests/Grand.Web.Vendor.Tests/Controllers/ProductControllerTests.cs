using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;
using Grand.Web.Vendor.Controllers;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Vendor.Tests.Controllers;

// Characterization tests for the tenant-isolation checks in ProductController, ahead of the planned
// consolidation of the near-duplicate ProductController/ProductViewModelService copies in
// Grand.Web.Admin / Grand.Web.Store / Grand.Web.Vendor. These lock down the *current* behavior
// (including the redirect target chosen on access denial) so the refactor has something to fail against.
[TestClass]
public class ProductControllerTests
{
    private const string OwnVendorId = "vendor-1";
    private const string OtherVendorId = "vendor-2";

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
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Domain.Vendors.Vendor { Id = OwnVendorId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _controller = new ProductController(
            _productViewModelServiceMock.Object,
            _productServiceMock.Object,
            new Mock<IInventoryManageService>().Object,
            contextAccessorMock.Object,
            new Mock<ILanguageService>().Object,
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
    public async Task Delete_ProductOwnedByAnotherVendor_RedirectsToListWithoutDeleting()
    {
        var product = new Product { Id = "p1", VendorId = OtherVendorId };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ProductOwnedByCurrentVendor_DeletesAndRedirectsToList()
    {
        var product = new Product { Id = "p1", VendorId = OwnVendorId };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(s => s.DeleteProduct(product), Times.Once);
    }

    [TestMethod]
    public async Task EditGet_ProductOwnedByAnotherVendor_RedirectsToListWithoutPreparingModel()
    {
        var product = new Product { Id = "p1", VendorId = OtherVendorId };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Edit("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductModel(It.IsAny<ProductModel>(), It.IsAny<Product>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task EditPost_ProductOwnedByAnotherVendor_RedirectsToListWithoutUpdating()
    {
        var product = new Product { Id = "p1", VendorId = OtherVendorId };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);

        var result = await _controller.Edit(new ProductModel { Id = "p1" }, continueEditing: false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductModel(It.IsAny<Product>(), It.IsAny<ProductModel>()), Times.Never);
    }
}
