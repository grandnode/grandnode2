using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Infrastructure;
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
public class BaseOrderControllerTests
{
    // BaseOrderController is abstract; minimal subclass so actions can be invoked directly.
    private class TestOrderController(
        IOrderViewModelService orderViewModelService,
        IOrderService orderService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IPdfService pdfService,
        IAdminDataScope<Order> scope)
        : BaseOrderController(orderViewModelService, orderService, translationService,
            contextAccessor, pdfService, scope)
    {
        public Task<(Order order, IActionResult denied)> LoadAuthorizedOrderPublic(string id) =>
            LoadAuthorizedOrder(id);
    }

    private TestOrderController _controller;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IOrderViewModelService> _orderViewModelServiceMock;
    private Mock<IAdminDataScope<Order>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _orderViewModelServiceMock = new Mock<IOrderViewModelService>();
        _scopeMock = new Mock<IAdminDataScope<Order>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        var contextAccessorMock = new Mock<IContextAccessor>();

        _controller = new TestOrderController(
            _orderViewModelServiceMock.Object,
            _orderServiceMock.Object,
            translationServiceMock.Object,
            contextAccessorMock.Object,
            new Mock<IPdfService>().Object,
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
    public async Task ListGet_CallsPrepareOrderListModel_WithScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _orderViewModelServiceMock
            .Setup(v => v.PrepareOrderListModel(null, null, null, null, "store-1", null))
            .ReturnsAsync(new OrderListModel());

        var result = await _controller.List();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        _orderViewModelServiceMock.Verify(v => v.PrepareOrderListModel(null, null, null, null, "store-1", null), Times.Once);
    }

    [TestMethod]
    public async Task ListGet_GlobalScope_PassesEmptyStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _orderViewModelServiceMock
            .Setup(v => v.PrepareOrderListModel(null, null, null, null, "", null))
            .ReturnsAsync(new OrderListModel());

        await _controller.List();

        _orderViewModelServiceMock.Verify(v => v.PrepareOrderListModel(null, null, null, null, "", null), Times.Once);
    }

    [TestMethod]
    public async Task ListPost_StoreScope_ForcesModelStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _orderViewModelServiceMock
            .Setup(v => v.PrepareOrderModel(It.IsAny<OrderListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<OrderModel>(), 0));

        var model = new OrderListModel { StoreId = "attacker-supplied" };
        await _controller.OrderList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.StoreId);
    }

    [TestMethod]
    public async Task ListPost_GlobalScope_LeavesSubmittedStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _orderViewModelServiceMock
            .Setup(v => v.PrepareOrderModel(It.IsAny<OrderListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<OrderModel>(), 0));

        var model = new OrderListModel { StoreId = "admin-submitted" };
        await _controller.OrderList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted", model.StoreId);
    }

    [TestMethod]
    public async Task ListPost_VendorScope_ForcesModelVendorId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-A");
        _orderViewModelServiceMock
            .Setup(v => v.PrepareOrderModel(It.IsAny<OrderListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<OrderModel>(), 0));

        var model = new OrderListModel();
        await _controller.OrderList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("vendor-A", model.VendorId);
    }

    [TestMethod]
    public async Task LoadAuthorizedOrder_NotFound_ReturnsRedirectToList()
    {
        _orderServiceMock.Setup(s => s.GetOrderById("missing")).ReturnsAsync((Order)null);

        var (order, denied) = await _controller.LoadAuthorizedOrderPublic("missing");

        Assert.IsNull(order);
        var redirect = denied as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task LoadAuthorizedOrder_ScopeDenies_ReturnsRedirectToList()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var (resultOrder, denied) = await _controller.LoadAuthorizedOrderPublic("o1");

        Assert.IsNull(resultOrder);
        var redirect = denied as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task LoadAuthorizedOrder_ScopeAllows_ReturnsOrderNoDenial()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var (resultOrder, denied) = await _controller.LoadAuthorizedOrderPublic("o1");

        Assert.AreSame(order, resultOrder);
        Assert.IsNull(denied);
    }
}
