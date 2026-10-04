using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Vendors;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseVendorReviewControllerTests
{
    // BaseVendorReviewController is abstract; minimal subclass so actions can be invoked directly.
    private class TestVendorReviewController(
        IVendorViewModelService vendorViewModelService,
        IVendorService vendorService,
        ITranslationService translationService,
        IAdminDataScope<VendorReview> scope)
        : BaseVendorReviewController(vendorViewModelService, vendorService, translationService, scope);

    private TestVendorReviewController _controller;
    private Mock<IVendorViewModelService> _vendorViewModelServiceMock;
    private Mock<IVendorService> _vendorServiceMock;
    private Mock<IAdminDataScope<VendorReview>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _vendorViewModelServiceMock = new Mock<IVendorViewModelService>();
        _vendorServiceMock = new Mock<IVendorService>();
        _scopeMock = new Mock<IAdminDataScope<VendorReview>>();
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _controller = new TestVendorReviewController(
            _vendorViewModelServiceMock.Object,
            _vendorServiceMock.Object,
            translationServiceMock.Object,
            _scopeMock.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public void Index_RedirectsToList()
    {
        var result = _controller.Index() as RedirectToActionResult;
        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public void List_Get_ReturnsViewWithEmptyModel()
    {
        var result = _controller.List() as ViewResult;
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result.Model, typeof(VendorReviewListModel));
    }

    [TestMethod]
    public async Task List_Post_GlobalScope_DoesNotForceSearchVendorId()
    {
        var model = new VendorReviewListModel { SearchVendorId = "caller-picked" };
        _vendorViewModelServiceMock
            .Setup(v => v.PrepareVendorReviewModel(model, 1, 10))
            .ReturnsAsync((new List<VendorReviewModel>(), 0));

        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("caller-picked", model.SearchVendorId);
        _vendorViewModelServiceMock.Verify(v => v.PrepareVendorReviewModel(model, 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task List_Post_VendorScope_ForcesSearchVendorId()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-A");
        var model = new VendorReviewListModel { SearchVendorId = "caller-picked" };
        _vendorViewModelServiceMock
            .Setup(v => v.PrepareVendorReviewModel(model, 1, 10))
            .ReturnsAsync((new List<VendorReviewModel>(), 0));

        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("vendor-A", model.SearchVendorId);
    }

    [TestMethod]
    public async Task Edit_Get_NotFound_RedirectsToList()
    {
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("missing")).ReturnsAsync((VendorReview)null);

        var result = await _controller.Edit("missing") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task Edit_Get_AccessDenied_RedirectsToList()
    {
        var review = new VendorReview { Id = "r1", VendorId = "vendor-OTHER" };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("r1")).ReturnsAsync(review);
        _scopeMock.Setup(s => s.HasAccess(review)).ReturnsAsync(false);

        var result = await _controller.Edit("r1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task Edit_Get_AccessGranted_ReturnsView()
    {
        var review = new VendorReview { Id = "r1", VendorId = "vendor-A" };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("r1")).ReturnsAsync(review);
        _scopeMock.Setup(s => s.HasAccess(review)).ReturnsAsync(true);

        var result = await _controller.Edit("r1") as ViewResult;

        Assert.IsNotNull(result);
        _vendorViewModelServiceMock.Verify(
            v => v.PrepareVendorReviewModel(It.IsAny<VendorReviewModel>(), review, false, false), Times.Once);
    }

    [TestMethod]
    public async Task Delete_AccessDenied_RedirectsToList()
    {
        var review = new VendorReview { Id = "r1", VendorId = "vendor-OTHER" };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("r1")).ReturnsAsync(review);
        _scopeMock.Setup(s => s.HasAccess(review)).ReturnsAsync(false);

        var result = await _controller.Delete("r1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _vendorViewModelServiceMock.Verify(v => v.DeleteVendorReview(It.IsAny<VendorReview>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_AccessGranted_DeletesAndRedirects()
    {
        var review = new VendorReview { Id = "r1", VendorId = "vendor-A" };
        _vendorServiceMock.Setup(v => v.GetVendorReviewById("r1")).ReturnsAsync(review);
        _scopeMock.Setup(s => s.HasAccess(review)).ReturnsAsync(true);

        var result = await _controller.Delete("r1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _vendorViewModelServiceMock.Verify(v => v.DeleteVendorReview(review), Times.Once);
    }

    [TestMethod]
    public async Task ApproveSelected_ForwardsSelectedIdsAndScope()
    {
        var ids = new List<string> { "r1:vendor-A" };

        await _controller.ApproveSelected(ids);

        _vendorViewModelServiceMock.Verify(v => v.ApproveVendorReviews(ids, _scopeMock.Object), Times.Once);
    }

    [TestMethod]
    public async Task DisapproveSelected_ForwardsSelectedIdsAndScope()
    {
        var ids = new List<string> { "r1:vendor-A" };

        await _controller.DisapproveSelected(ids);

        _vendorViewModelServiceMock.Verify(v => v.DisapproveVendorReviews(ids, _scopeMock.Object), Times.Once);
    }
}
