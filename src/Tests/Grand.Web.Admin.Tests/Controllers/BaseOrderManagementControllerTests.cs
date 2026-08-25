using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseOrderManagementControllerTests
{
    private class TestOrderManagementController(
        IOrderViewModelService orderViewModelService,
        IOrderService orderService,
        IOrderStatusService orderStatusService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IPdfService pdfService,
        IMediator mediator,
        IAdminDataScope<Order> scope)
        : BaseOrderManagementController(orderViewModelService, orderService, orderStatusService,
            translationService, contextAccessor, pdfService, mediator, scope);

    private TestOrderManagementController _controller;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IOrderViewModelService> _orderViewModelServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IAdminDataScope<Order>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _orderViewModelServiceMock = new Mock<IOrderViewModelService>();
        _mediatorMock = new Mock<IMediator>();
        _scopeMock = new Mock<IAdminDataScope<Order>>();

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _controller = new TestOrderManagementController(
            _orderViewModelServiceMock.Object, _orderServiceMock.Object,
            new Mock<IOrderStatusService>().Object, translationServiceMock.Object,
            new Mock<IContextAccessor>().Object, new Mock<IPdfService>().Object,
            _mediatorMock.Object, _scopeMock.Object);

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
    public async Task CancelOrder_ScopeDenies_RedirectsToList_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.CancelOrder("o1");

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<CancelOrderCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task CancelOrder_Authorized_SendsCancelCommand_RedirectsToEdit()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.CancelOrder("o1");

        var redirect = result as RedirectToActionResult;
        Assert.AreEqual("Edit", redirect?.ActionName);
        _mediatorMock.Verify(m => m.Send(
            It.Is<CancelOrderCommand>(c => c.Order == order && c.NotifyCustomer), default), Times.Once);
    }

    [TestMethod]
    public async Task ChangeOrderStatus_ScopeDenies_RedirectsToList_NoUpdate()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.ChangeOrderStatus("o1", new OrderModel { OrderStatusId = 30 });

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _orderServiceMock.Verify(s => s.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ScopeDenies_RedirectsToList_NoDeleteCommand()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.Delete(new OrderDeleteModel("o1"));

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteOrderCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task Delete_Authorized_ValidModel_SendsDeleteCommand()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.Delete(new OrderDeleteModel("o1"));

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(
            It.Is<DeleteOrderCommand>(c => c.Order == order), default), Times.Once);
    }

    [TestMethod]
    public async Task EditOrderTotals_ScopeDenies_RedirectsToList_NoUpdate()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.EditOrderTotals("o1", new OrderModel());

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _orderServiceMock.Verify(s => s.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task EditOrderTotals_Authorized_UpdatesOrderAndInsertsNote()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var model = new OrderModel { OrderTotalValue = 99.0, CurrencyRate = 1.0 };
        var result = await _controller.EditOrderTotals("o1", model);

        Assert.AreEqual("o1", (result as RedirectToActionResult)?.RouteValues["id"]);
        Assert.AreEqual(99.0, order.OrderTotal);
        _orderServiceMock.Verify(s => s.InsertOrderNote(It.Is<OrderNote>(n => n.Note == "Order totals have been edited")), Times.Once);
    }

    [TestMethod]
    public async Task SaveOrderItem_ScopeDenies_RedirectsToList_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.SaveOrderItem("o1", new OrderItemsModel(new List<OrderItemModel>(), "i1"));

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task SaveOrderItem_OrderCancelled_ErrorsWithoutSendingCommand()
    {
        var order = new Order { Id = "o1", OrderStatusId = (int)OrderStatusSystem.Cancelled };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.SaveOrderItem("o1", new OrderItemsModel(new List<OrderItemModel>(), "i1"));

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task DeleteOrderItem_ScopeDenies_RedirectsToList_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.DeleteOrderItem("o1", "i1");

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteOrderItemCommand>(), default), Times.Never);
    }
}
