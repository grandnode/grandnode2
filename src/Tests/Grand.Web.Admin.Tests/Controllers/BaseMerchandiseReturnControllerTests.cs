using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
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
public class BaseMerchandiseReturnControllerTests
{
    // BaseMerchandiseReturnController is abstract; minimal subclass so actions can be invoked
    // directly. NotFoundOrDeniedForNotesSelect (Task 8) uses the base's default (throw) here - Store's
    // own override is tested separately in Task 8's tests.
    protected class TestMerchandiseReturnController(
        IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
        ITranslationService translationService,
        IMerchandiseReturnService merchandiseReturnService,
        IOrderService orderService,
        IAdminDataScope<MerchandiseReturn> scope)
        : BaseMerchandiseReturnController(merchandiseReturnViewModelService, translationService,
            merchandiseReturnService, orderService, scope);

    protected TestMerchandiseReturnController _controller;
    protected Mock<IMerchandiseReturnService> _merchandiseReturnServiceMock;
    protected Mock<IMerchandiseReturnViewModelService> _merchandiseReturnViewModelServiceMock;
    protected Mock<IOrderService> _orderServiceMock;
    protected Mock<IAdminDataScope<MerchandiseReturn>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnViewModelServiceMock = new Mock<IMerchandiseReturnViewModelService>();
        _orderServiceMock = new Mock<IOrderService>();
        _scopeMock = new Mock<IAdminDataScope<MerchandiseReturn>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _controller = new TestMerchandiseReturnController(
            _merchandiseReturnViewModelServiceMock.Object,
            translationServiceMock.Object,
            _merchandiseReturnServiceMock.Object,
            _orderServiceMock.Object,
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
    public void ListGet_ReturnsViewWithPrepareReturnRequestListModelResult()
    {
        var model = new MerchandiseReturnListModel();
        _merchandiseReturnViewModelServiceMock.Setup(v => v.PrepareReturnRequestListModel()).Returns(model);

        var result = _controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
    }

    [TestMethod]
    public async Task ListPost_StoreScope_ForcesModelStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _merchandiseReturnViewModelServiceMock
            .Setup(v => v.PrepareMerchandiseReturnModel(It.IsAny<MerchandiseReturnListModel>(), 1, 10))
            .ReturnsAsync((new List<MerchandiseReturnModel>(), 0));

        var model = new MerchandiseReturnListModel { StoreId = "attacker-supplied" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.StoreId);
    }

    [TestMethod]
    public async Task ListPost_GlobalScope_LeavesSubmittedStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _merchandiseReturnViewModelServiceMock
            .Setup(v => v.PrepareMerchandiseReturnModel(It.IsAny<MerchandiseReturnListModel>(), 1, 10))
            .ReturnsAsync((new List<MerchandiseReturnModel>(), 0));

        var model = new MerchandiseReturnListModel { StoreId = "admin-submitted" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted", model.StoreId);
    }

    [TestMethod]
    public async Task ListPost_VendorScope_LeavesStoreIdUntouched_ScopingHappensInsideService()
    {
        // Vendor has no store concept - List(POST) never forces model.StoreId for Vendor (spec §5);
        // vendor-scoping happens inside the shared service call via scope.DefaultVendorId (Task 4).
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-A");
        _merchandiseReturnViewModelServiceMock
            .Setup(v => v.PrepareMerchandiseReturnModel(It.IsAny<MerchandiseReturnListModel>(), 1, 10))
            .ReturnsAsync((new List<MerchandiseReturnModel>(), 0));

        var model = new MerchandiseReturnListModel();
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.IsNull(model.StoreId);
    }
}
