using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class OrderPlacedOrderApprovalHandlerTests
{
    private OrderPlacedOrderApprovalHandler _handler;
    private Mock<IOrderApprovalService> _orderApprovalServiceMock;

    [TestInitialize]
    public void Init()
    {
        _orderApprovalServiceMock = new Mock<IOrderApprovalService>();
        _handler = new OrderPlacedOrderApprovalHandler(_orderApprovalServiceMock.Object);
    }

    [TestMethod]
    public async Task Handle_ActiveApproved_MarkedUsedWithOrderId()
    {
        //Arrange
        var approval = new OrderApproval { Status = OrderApprovalStatus.Approved };
        var order = new Order { Id = "order1", CustomerId = "customer1", StoreId = "store1" };
        _orderApprovalServiceMock.Setup(x => x.GetActive("customer1", "store1")).ReturnsAsync(approval);

        //Act
        await _handler.Handle(new OrderPlacedEvent(order), CancellationToken.None);

        //Assert
        Assert.AreEqual(OrderApprovalStatus.Used, approval.Status);
        Assert.AreEqual("order1", approval.OrderId);
        _orderApprovalServiceMock.Verify(x => x.Update(approval), Times.Once);
    }

    [TestMethod]
    public async Task Handle_OnlyPending_Untouched()
    {
        //Arrange
        var approval = new OrderApproval { Status = OrderApprovalStatus.Pending };
        var order = new Order { Id = "order1", CustomerId = "customer1", StoreId = "store1" };
        _orderApprovalServiceMock.Setup(x => x.GetActive("customer1", "store1")).ReturnsAsync(approval);

        //Act
        await _handler.Handle(new OrderPlacedEvent(order), CancellationToken.None);

        //Assert
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        Assert.IsNull(approval.OrderId);
        _orderApprovalServiceMock.Verify(x => x.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_NoActive_NoWrite()
    {
        //Arrange
        var order = new Order { Id = "order1", CustomerId = "customer1", StoreId = "store1" };
        _orderApprovalServiceMock.Setup(x => x.GetActive("customer1", "store1")).ReturnsAsync((OrderApproval)null);

        //Act
        await _handler.Handle(new OrderPlacedEvent(order), CancellationToken.None);

        //Assert
        _orderApprovalServiceMock.Verify(x => x.Update(It.IsAny<OrderApproval>()), Times.Never);
    }
}
