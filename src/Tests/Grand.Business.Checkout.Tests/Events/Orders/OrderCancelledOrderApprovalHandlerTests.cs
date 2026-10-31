using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class OrderCancelledOrderApprovalHandlerTests
{
    private Mock<IOrderApprovalService> _approvalService;
    private OrderCancelledOrderApprovalHandler _handler;
    private Mock<ILogger<OrderCancelledOrderApprovalHandler>> _logger;
    private Order _order;

    [TestInitialize]
    public void Init()
    {
        _approvalService = new Mock<IOrderApprovalService>();
        _logger = new Mock<ILogger<OrderCancelledOrderApprovalHandler>>();
        _handler = new OrderCancelledOrderApprovalHandler(_approvalService.Object, _logger.Object);
        _order = new Order { Id = "order", OrderNumber = 7, OrderStatusId = (int)OrderStatusSystem.Cancelled };
    }

    private void NothingWasClosed()
    {
        _approvalService.Verify(x => x.Close(It.IsAny<OrderApproval>(), It.IsAny<OrderApprovalStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PendingApproval_ClosesItAsCancelled()
    {
        var approval = new OrderApproval { Id = "a1", OrderId = "order", Status = OrderApprovalStatus.Pending };
        _approvalService.Setup(x => x.GetByOrderId("order")).ReturnsAsync(approval);

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _approvalService.Verify(x => x.Close(approval, OrderApprovalStatus.Cancelled, null, null), Times.Once);
    }

    [TestMethod]
    [DataRow(OrderApprovalStatus.Approved)]
    [DataRow(OrderApprovalStatus.Rejected)]
    [DataRow(OrderApprovalStatus.Withdrawn)]
    [DataRow(OrderApprovalStatus.Cancelled)]
    public async Task Handle_DecidedApproval_IsLeftAlone(OrderApprovalStatus status)
    {
        var approval = new OrderApproval { Id = "a1", OrderId = "order", Status = status };
        _approvalService.Setup(x => x.GetByOrderId("order")).ReturnsAsync(approval);

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        Assert.AreEqual(status, approval.Status);
        NothingWasClosed();
    }

    [TestMethod]
    public async Task Handle_OrderWithoutApproval_DoesNothing()
    {
        _approvalService.Setup(x => x.GetByOrderId("order")).ReturnsAsync((OrderApproval)null);

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        NothingWasClosed();
    }

    [TestMethod]
    public async Task Handle_SaveFails_LogsAndCompletes()
    {
        _approvalService.Setup(x => x.GetByOrderId("order"))
            .ReturnsAsync(new OrderApproval { Id = "a1", OrderId = "order", Status = OrderApprovalStatus.Pending });
        _approvalService.Setup(x => x.Close(It.IsAny<OrderApproval>(), OrderApprovalStatus.Cancelled, null, null))
            .ThrowsAsync(new Exception("db down"));

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}
