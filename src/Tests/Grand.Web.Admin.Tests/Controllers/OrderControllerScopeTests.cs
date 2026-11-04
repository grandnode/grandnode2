using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class OrderControllerScopeTests
{
    private readonly Order _allowed = new() { Id = "o1" };
    private readonly Order _denied = new() { Id = "o2" };
    private OrderController _controller;
    private Mock<IExportManager<Order>> _exportManagerMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IShipmentService> _shipmentServiceMock;

    [TestInitialize]
    public void Setup()
    {
        //GetOrdersByIds returns whatever the request names; the scope decides what the staff member may touch
        var orderServiceMock = new Mock<IOrderService>();
        orderServiceMock.Setup(s => s.GetOrdersByIds(It.IsAny<string[]>()))
            .ReturnsAsync((IList<Order>)new List<Order> { _allowed, _denied });
        var scopeMock = new Mock<IAdminDataScope<Order>>();
        scopeMock.Setup(s => s.HasAccess(_allowed)).ReturnsAsync(true);
        scopeMock.Setup(s => s.HasAccess(_denied)).ReturnsAsync(false);

        _exportManagerMock = new Mock<IExportManager<Order>>();
        _exportManagerMock.Setup(e => e.Export(It.IsAny<IEnumerable<Order>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _mediatorMock = new Mock<IMediator>();
        _shipmentServiceMock = new Mock<IShipmentService>();
        _shipmentServiceMock.Setup(s => s.GetShipmentsByOrder(It.IsAny<string>()))
            .ReturnsAsync((IList<Shipment>)new List<Shipment>());

        _controller = new OrderController(
            new Mock<IOrderViewModelService>().Object,
            orderServiceMock.Object,
            new Mock<IOrderStatusService>().Object,
            new Mock<IRequestTranslationService>().Object,
            new Mock<IContextAccessor>().Object,
            new Mock<IPdfService>().Object,
            _mediatorMock.Object,
            scopeMock.Object,
            new Mock<IOrderNoteViewModelService>().Object,
            new Mock<IOrderAddressViewModelService>().Object,
            new Mock<IOrderProductViewModelService>().Object,
            _exportManagerMock.Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TestMethod]
    public async Task ExportExcelSelected_ScopeDenies_ExcludesOrderFromExport()
    {
        await _controller.ExportExcelSelected("o1,o2");

        _exportManagerMock.Verify(e => e.Export(It.Is<IEnumerable<Order>>(
            orders => orders.Count() == 1 && orders.First() == _allowed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteSelected_ScopeDenies_DoesNotDeleteOrder()
    {
        await _controller.DeleteSelected(["o1", "o2"], _shipmentServiceMock.Object);

        _mediatorMock.Verify(m => m.Send(It.Is<DeleteOrderCommand>(c => c.Order == _allowed),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<DeleteOrderCommand>(c => c.Order == _denied),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
