using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Mediator;
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
public class BaseShipmentControllerTests
{
    // BaseShipmentController is abstract; minimal subclass so actions can be invoked directly.
    private class TestShipmentController(
        IShipmentViewModelService shipmentViewModelService,
        IOrderService orderService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IPdfService pdfService,
        IShipmentService shipmentService,
        IDateTimeService dateTimeService,
        IMediator mediator,
        IAdminDataScope<Shipment> scope,
        IAdminDataScope<Order> orderScope)
        : BaseShipmentController(shipmentViewModelService, orderService, translationService,
            contextAccessor, pdfService, shipmentService, dateTimeService, mediator, scope, orderScope)
    {
        public Task<(Shipment shipment, IActionResult denied)> LoadAuthorizedShipmentPublic(string id) =>
            LoadAuthorizedShipment(id);
    }

    private TestShipmentController _controller;
    private Mock<IShipmentViewModelService> _shipmentViewModelServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IShipmentService> _shipmentServiceMock;
    private Mock<IAdminDataScope<Shipment>> _scopeMock;
    private Mock<IAdminDataScope<Order>> _orderScopeMock;

    [TestInitialize]
    public void Setup()
    {
        _shipmentViewModelServiceMock = new Mock<IShipmentViewModelService>();
        _orderServiceMock = new Mock<IOrderService>();
        _shipmentServiceMock = new Mock<IShipmentService>();
        _scopeMock = new Mock<IAdminDataScope<Shipment>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);
        _orderScopeMock = new Mock<IAdminDataScope<Order>>();
        _orderScopeMock.Setup(s => s.HasAccess(It.IsAny<Order>())).ReturnsAsync(true);

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        var contextAccessorMock = new Mock<IContextAccessor>();
        var pdfServiceMock = new Mock<IPdfService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        var mediatorMock = new Mock<IMediator>();

        _controller = new TestShipmentController(
            _shipmentViewModelServiceMock.Object,
            _orderServiceMock.Object,
            translationServiceMock.Object,
            contextAccessorMock.Object,
            pdfServiceMock.Object,
            _shipmentServiceMock.Object,
            dateTimeServiceMock.Object,
            mediatorMock.Object,
            _scopeMock.Object,
            _orderScopeMock.Object);

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
    public async Task List_ReturnsViewWithPreparedModel()
    {
        var model = new ShipmentListModel();
        _shipmentViewModelServiceMock.Setup(v => v.PrepareShipmentListModel()).ReturnsAsync(model);

        var result = await _controller.List();

        var viewResult = result as ViewResult;
        Assert.IsNotNull(viewResult);
        Assert.AreSame(model, viewResult.Model);
        _shipmentViewModelServiceMock.Verify(v => v.PrepareShipmentListModel(), Times.Once);
    }

    [TestMethod]
    public async Task ShipmentListSelect_GlobalScope_DoesNotForceStoreOrVendorId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipments(It.IsAny<ShipmentListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<Shipment>(), 0));

        var model = new ShipmentListModel { StoreId = "submitted-store", VendorId = "submitted-vendor" };
        await _controller.ShipmentListSelect(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("submitted-store", model.StoreId);
        Assert.AreEqual("submitted-vendor", model.VendorId);
    }

    [TestMethod]
    public async Task ShipmentListSelect_StoreScope_ForcesStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipments(It.IsAny<ShipmentListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<Shipment>(), 0));

        var model = new ShipmentListModel { StoreId = "attacker-supplied" };
        await _controller.ShipmentListSelect(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.StoreId);
    }

    [TestMethod]
    public async Task ShipmentListSelect_VendorScope_ForcesVendorId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-1");
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipments(It.IsAny<ShipmentListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<Shipment>(), 0));

        var model = new ShipmentListModel { VendorId = "attacker-supplied" };
        await _controller.ShipmentListSelect(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("vendor-1", model.VendorId);
    }

    [TestMethod]
    public async Task ShipmentsByOrder_FiltersToAccessibleShipmentsOnly()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);

        var accessibleShipment = new Shipment { Id = "s1", OrderId = "o1", CreatedOnUtc = DateTime.UtcNow };
        var deniedShipment = new Shipment { Id = "s2", OrderId = "o1", CreatedOnUtc = DateTime.UtcNow.AddMinutes(1) };
        _shipmentServiceMock.Setup(s => s.GetShipmentsByOrder("o1"))
            .ReturnsAsync((IList<Shipment>)new List<Shipment> { accessibleShipment, deniedShipment });

        _scopeMock.Setup(s => s.HasAccess(accessibleShipment)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(deniedShipment)).ReturnsAsync(false);

        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipmentModel(accessibleShipment, false, false))
            .ReturnsAsync(new ShipmentModel { Id = "s1" });

        var result = await _controller.ShipmentsByOrder("o1", new DataSourceRequest());

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        var gridModel = jsonResult.Value as DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
        var data = gridModel.Data as List<ShipmentModel>;
        Assert.IsNotNull(data);
        Assert.AreEqual(1, data.Count);
        Assert.AreEqual("s1", data[0].Id);
        _shipmentViewModelServiceMock.Verify(v => v.PrepareShipmentModel(deniedShipment, false, false), Times.Never);
    }

    [TestMethod]
    public async Task ShipmentsItemsByShipmentId_DeniedAccess_Throws()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(false);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _controller.ShipmentsItemsByShipmentId("s1", new DataSourceRequest()));
    }

    [TestMethod]
    public async Task AddShipmentGet_OrderNotFound_RedirectsToList()
    {
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync((Order)null);

        var result = await _controller.AddShipment("o1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _shipmentViewModelServiceMock.Verify(v => v.PrepareShipmentModel(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task AddShipmentGet_OrderDenied_RedirectsToList()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _orderScopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.AddShipment("o1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _shipmentViewModelServiceMock.Verify(v => v.PrepareShipmentModel(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task AddShipmentPost_NoItemsSelected_ShowsErrorAndRedirects()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _orderScopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.FilterOrderItems(It.IsAny<IEnumerable<OrderItem>>()))
            .Returns((IEnumerable<OrderItem> items) => items);

        var emptyShipment = new Shipment { Id = "s1" };
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipment(order, It.IsAny<IEnumerable<OrderItem>>(), It.IsAny<AddShipmentModel>()))
            .ReturnsAsync((emptyShipment, (double?)null));

        var model = new AddShipmentModel { OrderId = "o1" };
        var result = await _controller.AddShipment(model, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("AddShipment", redirect.ActionName);
        Assert.AreEqual("o1", redirect.RouteValues["orderId"]);
        _shipmentServiceMock.Verify(s => s.InsertShipment(It.IsAny<Shipment>()), Times.Never);
        _shipmentViewModelServiceMock.Verify(v => v.ValidStockShipment(It.IsAny<Shipment>()), Times.Never);
    }

    [TestMethod]
    public async Task AddShipmentPost_OutOfStock_ShowsErrorAndRedirects()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _orderScopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.FilterOrderItems(It.IsAny<IEnumerable<OrderItem>>()))
            .Returns((IEnumerable<OrderItem> items) => items);

        var shipment = new Shipment { Id = "s1" };
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "oi1" });
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipment(order, It.IsAny<IEnumerable<OrderItem>>(), It.IsAny<AddShipmentModel>()))
            .ReturnsAsync((shipment, (double?)10));
        _shipmentViewModelServiceMock
            .Setup(v => v.ValidStockShipment(shipment))
            .ReturnsAsync((false, "Out of stock"));

        var model = new AddShipmentModel { OrderId = "o1" };
        var result = await _controller.AddShipment(model, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("AddShipment", redirect.ActionName);
        Assert.AreEqual("o1", redirect.RouteValues["orderId"]);
        _shipmentServiceMock.Verify(s => s.InsertShipment(It.IsAny<Shipment>()), Times.Never);
    }

    [TestMethod]
    public async Task AddShipmentPost_Success_ContinueEditing_RedirectsToShipmentDetails()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _orderScopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.FilterOrderItems(It.IsAny<IEnumerable<OrderItem>>()))
            .Returns((IEnumerable<OrderItem> items) => items);

        var shipment = new Shipment { Id = "s1" };
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "oi1" });
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipment(order, It.IsAny<IEnumerable<OrderItem>>(), It.IsAny<AddShipmentModel>()))
            .ReturnsAsync((shipment, (double?)10));
        _shipmentViewModelServiceMock
            .Setup(v => v.ValidStockShipment(shipment))
            .ReturnsAsync((true, (string)null));

        var model = new AddShipmentModel { OrderId = "o1" };
        var result = await _controller.AddShipment(model, true);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        Assert.AreEqual(10, shipment.TotalWeight);
        _shipmentServiceMock.Verify(s => s.InsertShipment(shipment), Times.Once);
        _orderServiceMock.Verify(s => s.InsertOrderNote(It.Is<OrderNote>(n => n.OrderId == "o1")), Times.Once);
    }

    [TestMethod]
    public async Task AddShipmentPost_Success_NotContinueEditing_RedirectsToList()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _orderScopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.FilterOrderItems(It.IsAny<IEnumerable<OrderItem>>()))
            .Returns((IEnumerable<OrderItem> items) => items);

        var shipment = new Shipment { Id = "s1" };
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "oi1" });
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipment(order, It.IsAny<IEnumerable<OrderItem>>(), It.IsAny<AddShipmentModel>()))
            .ReturnsAsync((shipment, (double?)10));
        _shipmentViewModelServiceMock
            .Setup(v => v.ValidStockShipment(shipment))
            .ReturnsAsync((true, (string)null));

        var model = new AddShipmentModel { OrderId = "o1" };
        var result = await _controller.AddShipment(model, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _shipmentServiceMock.Verify(s => s.InsertShipment(shipment), Times.Once);
    }

    [TestMethod]
    public async Task AddShipmentPost_FiltersOrderItemsThroughScope()
    {
        var itemKept = new OrderItem { Id = "oi1" };
        var itemFiltered = new OrderItem { Id = "oi2" };
        var order = new Order { Id = "o1" };
        order.OrderItems.Add(itemKept);
        order.OrderItems.Add(itemFiltered);
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _orderScopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var filtered = new List<OrderItem> { itemKept };
        _scopeMock.Setup(s => s.FilterOrderItems(order.OrderItems)).Returns(filtered);

        var shipment = new Shipment { Id = "s1" };
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipment(order, filtered, It.IsAny<AddShipmentModel>()))
            .ReturnsAsync((shipment, (double?)null));

        var model = new AddShipmentModel { OrderId = "o1" };
        await _controller.AddShipment(model, false);

        _scopeMock.Verify(s => s.FilterOrderItems(order.OrderItems), Times.Once);
        _shipmentViewModelServiceMock.Verify(
            v => v.PrepareShipment(order, filtered, It.IsAny<AddShipmentModel>()), Times.Once);
    }
}
