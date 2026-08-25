using Grand.Business.Core.Commands.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Localization;
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
    private Mock<IMediator> _mediatorMock;

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
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "lang-1" });
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        var pdfServiceMock = new Mock<IPdfService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        _mediatorMock = new Mock<IMediator>();

        _controller = new TestShipmentController(
            _shipmentViewModelServiceMock.Object,
            _orderServiceMock.Object,
            translationServiceMock.Object,
            contextAccessorMock.Object,
            pdfServiceMock.Object,
            _shipmentServiceMock.Object,
            dateTimeServiceMock.Object,
            _mediatorMock.Object,
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

    [TestMethod]
    public async Task ShipmentDetails_Denied_RedirectsToList()
    {
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync((Shipment)null);

        var result = await _controller.ShipmentDetails("s1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ShipmentDetails_Authorized_ReturnsViewWithModel()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);

        var model = new ShipmentModel { Id = "s1" };
        _shipmentViewModelServiceMock.Setup(v => v.PrepareShipmentModel(shipment, true, true)).ReturnsAsync(model);

        var result = await _controller.ShipmentDetails("s1");

        var viewResult = result as ViewResult;
        Assert.IsNotNull(viewResult);
        Assert.AreSame(model, viewResult.Model);
    }

    [TestMethod]
    public async Task DeleteShipment_Authorized_DeletesAndAddsOrderNote()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1", ShipmentNumber = 5 };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);

        var result = await _controller.DeleteShipment("s1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("Order", redirect.ControllerName);
        Assert.AreEqual("o1", redirect.RouteValues["Id"]);
        _shipmentServiceMock.Verify(s => s.DeleteShipment(shipment), Times.Once);
        _orderServiceMock.Verify(s => s.InsertOrderNote(It.Is<OrderNote>(n => n.OrderId == "o1")), Times.Once);
    }

    [TestMethod]
    public async Task SetTrackingNumber_Denied_RedirectsToList()
    {
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync((Shipment)null);

        var result = await _controller.SetTrackingNumber(new ShipmentTrackingModel("s1", "TRACK1"));

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _shipmentServiceMock.Verify(s => s.UpdateShipment(It.IsAny<Shipment>()), Times.Never);
    }

    [TestMethod]
    public async Task SetTrackingNumber_Authorized_UpdatesShipment()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var result = await _controller.SetTrackingNumber(new ShipmentTrackingModel("s1", "TRACK1"));

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        Assert.AreEqual("TRACK1", shipment.TrackingNumber);
        _shipmentServiceMock.Verify(s => s.UpdateShipment(shipment), Times.Once);
    }

    [TestMethod]
    public async Task SetShipmentAdminComment_Authorized_UpdatesShipment()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var result = await _controller.SetShipmentAdminComment(new ShipmentAdminCommentModel("s1", "a comment"));

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        Assert.AreEqual("a comment", shipment.AdminComment);
        _shipmentServiceMock.Verify(s => s.UpdateShipment(shipment), Times.Once);
    }

    [TestMethod]
    public async Task SetAsShipped_MediatorThrows_ShowsErrorAndRedirects()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        _mediatorMock.Setup(m => m.Send(It.IsAny<ShipCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var result = await _controller.SetAsShipped("s1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
    }

    [TestMethod]
    public async Task SetAsShipped_Success_RedirectsToShipmentDetails()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        _mediatorMock.Setup(m => m.Send(It.IsAny<ShipCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.SetAsShipped("s1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        _mediatorMock.Verify(m => m.Send(It.Is<ShipCommand>(c => c.Shipment == shipment && c.NotifyCustomer), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task EditShippedDate_MissingDate_ShowsErrorAndRedirects()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var result = await _controller.EditShippedDate(new ShipmentShippedDateModel("s1", null));

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        _shipmentServiceMock.Verify(s => s.UpdateShipment(It.IsAny<Shipment>()), Times.Never);
    }

    [TestMethod]
    public async Task EditShippedDate_ValidDate_UpdatesAndRedirects()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var shippedDate = DateTime.UtcNow;
        var result = await _controller.EditShippedDate(new ShipmentShippedDateModel("s1", shippedDate));

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        Assert.AreEqual(shippedDate, shipment.ShippedDateUtc);
        _shipmentServiceMock.Verify(s => s.UpdateShipment(shipment), Times.Once);
    }

    [TestMethod]
    public async Task SetAsDelivered_Success_RedirectsToShipmentDetails()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeliveryCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.SetAsDelivered("s1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        _mediatorMock.Verify(m => m.Send(It.Is<DeliveryCommand>(c => c.Shipment == shipment && c.NotifyCustomer), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task EditDeliveryDate_ValidDate_UpdatesAndRedirects()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var deliveryDate = DateTime.UtcNow;
        var result = await _controller.EditDeliveryDate(new ShipmentDeliveryDateModel("s1", deliveryDate));

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("ShipmentDetails", redirect.ActionName);
        Assert.AreEqual("s1", redirect.RouteValues["id"]);
        Assert.AreEqual(deliveryDate, shipment.DeliveryDateUtc);
        _shipmentServiceMock.Verify(s => s.UpdateShipment(shipment), Times.Once);
    }

    [TestMethod]
    public async Task PdfPackagingSlip_Denied_RedirectsToList()
    {
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync((Shipment)null);

        var result = await _controller.PdfPackagingSlip("s1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _orderServiceMock.Verify(s => s.GetOrderById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PdfPackagingSlipAll_NoShipments_ShowsErrorAndRedirects()
    {
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipments(It.IsAny<ShipmentListModel>(), 1, 100))
            .ReturnsAsync((Enumerable.Empty<Shipment>(), 0));

        var result = await _controller.PdfPackagingSlipAll(new ShipmentListModel());

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task PdfPackagingSlipAll_ForcesStoreAndVendorIdConditionally()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-1");
        _shipmentViewModelServiceMock
            .Setup(v => v.PrepareShipments(It.IsAny<ShipmentListModel>(), 1, 100))
            .ReturnsAsync((Enumerable.Empty<Shipment>(), 0));

        var model = new ShipmentListModel { StoreId = "attacker-store", VendorId = "attacker-vendor" };
        await _controller.PdfPackagingSlipAll(model);

        Assert.AreEqual("store-1", model.StoreId);
        Assert.AreEqual("vendor-1", model.VendorId);
    }

    [TestMethod]
    public async Task PdfPackagingSlipSelected_FiltersToAccessibleShipments()
    {
        var accessibleShipment = new Shipment { Id = "s1" };
        var deniedShipment = new Shipment { Id = "s2" };
        _shipmentServiceMock
            .Setup(s => s.GetShipmentsByIds(new[] { "s1", "s2" }))
            .ReturnsAsync((IList<Shipment>)new List<Shipment> { accessibleShipment, deniedShipment });
        _scopeMock.Setup(s => s.HasAccess(accessibleShipment)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(deniedShipment)).ReturnsAsync(false);

        var result = await _controller.PdfPackagingSlipSelected("s1,s2");

        var fileResult = result as FileContentResult;
        Assert.IsNotNull(fileResult);
        Assert.AreEqual("packagingslips.pdf", fileResult.FileDownloadName);
        _scopeMock.Verify(s => s.HasAccess(accessibleShipment), Times.Once);
        _scopeMock.Verify(s => s.HasAccess(deniedShipment), Times.Once);
    }

    [TestMethod]
    public async Task SetAsShippedSelected_FiltersToAccessibleShipments_IgnoresPerItemExceptions()
    {
        var accessibleShipment1 = new Shipment { Id = "s1" };
        var accessibleShipment2 = new Shipment { Id = "s2" };
        var deniedShipment = new Shipment { Id = "s3" };
        _shipmentServiceMock
            .Setup(s => s.GetShipmentsByIds(new[] { "s1", "s2", "s3" }))
            .ReturnsAsync((IList<Shipment>)new List<Shipment> { accessibleShipment1, accessibleShipment2, deniedShipment });
        _scopeMock.Setup(s => s.HasAccess(accessibleShipment1)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(accessibleShipment2)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(deniedShipment)).ReturnsAsync(false);

        _mediatorMock
            .Setup(m => m.Send(It.Is<ShipCommand>(c => c.Shipment == accessibleShipment1), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));
        _mediatorMock
            .Setup(m => m.Send(It.Is<ShipCommand>(c => c.Shipment == accessibleShipment2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.SetAsShippedSelected(new List<string> { "s1", "s2", "s3" });

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        _mediatorMock.Verify(m => m.Send(It.Is<ShipCommand>(c => c.Shipment == accessibleShipment1), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<ShipCommand>(c => c.Shipment == accessibleShipment2), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<ShipCommand>(c => c.Shipment == deniedShipment), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SetAsDeliveredSelected_FiltersToAccessibleShipments()
    {
        var accessibleShipment = new Shipment { Id = "s1" };
        var deniedShipment = new Shipment { Id = "s2" };
        _shipmentServiceMock
            .Setup(s => s.GetShipmentsByIds(new[] { "s1", "s2" }))
            .ReturnsAsync((IList<Shipment>)new List<Shipment> { accessibleShipment, deniedShipment });
        _scopeMock.Setup(s => s.HasAccess(accessibleShipment)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(deniedShipment)).ReturnsAsync(false);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeliveryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.SetAsDeliveredSelected(new List<string> { "s1", "s2" });

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        _mediatorMock.Verify(m => m.Send(It.Is<DeliveryCommand>(c => c.Shipment == accessibleShipment), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<DeliveryCommand>(c => c.Shipment == deniedShipment), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ShipmentNotesSelect_Denied_Throws()
    {
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync((Shipment)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _controller.ShipmentNotesSelect("s1", new DataSourceRequest()));
    }

    [TestMethod]
    public async Task ShipmentNotesSelect_Authorized_ReturnsNotes()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var notes = new List<ShipmentModel.ShipmentNote> { new() { Id = "n1" } };
        _shipmentViewModelServiceMock.Setup(v => v.PrepareShipmentNotes(shipment)).ReturnsAsync(notes);

        var result = await _controller.ShipmentNotesSelect("s1", new DataSourceRequest());

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        var gridModel = jsonResult.Value as DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
        Assert.AreSame(notes, gridModel.Data);
    }

    [TestMethod]
    public async Task ShipmentNoteAdd_Denied_ReturnsResultFalse()
    {
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync((Shipment)null);

        var result = await _controller.ShipmentNoteAdd("s1", "download-1", true, "hello");

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        var value = jsonResult.Value;
        var resultProperty = value.GetType().GetProperty("Result");
        Assert.IsNotNull(resultProperty);
        Assert.AreEqual(false, resultProperty.GetValue(value));
        _shipmentViewModelServiceMock.Verify(
            v => v.InsertShipmentNote(It.IsAny<Shipment>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ShipmentNoteAdd_Authorized_PassesDownloadIdThrough()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var result = await _controller.ShipmentNoteAdd("s1", "download-1", true, "hello");

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        var value = jsonResult.Value;
        var resultProperty = value.GetType().GetProperty("Result");
        Assert.IsNotNull(resultProperty);
        Assert.AreEqual(true, resultProperty.GetValue(value));
        _shipmentViewModelServiceMock.Verify(
            v => v.InsertShipmentNote(shipment, "download-1", true, "hello"), Times.Once);
    }

    [TestMethod]
    public async Task ShipmentNoteDelete_Denied_Throws()
    {
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync((Shipment)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _controller.ShipmentNoteDelete("n1", "s1"));
    }

    [TestMethod]
    public async Task ShipmentNoteDelete_Authorized_DeletesNote()
    {
        var shipment = new Shipment { Id = "s1", OrderId = "o1" };
        _shipmentServiceMock.Setup(s => s.GetShipmentById("s1")).ReturnsAsync(shipment);
        _scopeMock.Setup(s => s.HasAccess(shipment)).ReturnsAsync(true);

        var result = await _controller.ShipmentNoteDelete("n1", "s1");

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        Assert.AreEqual("", jsonResult.Value);
        _shipmentViewModelServiceMock.Verify(v => v.DeleteShipmentNote(shipment, "n1"), Times.Once);
    }
}
