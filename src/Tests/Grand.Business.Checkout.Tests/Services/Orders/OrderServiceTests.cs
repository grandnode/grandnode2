using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Grand.SharedKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<OrderNote>> _orderNoteRepositoryMock;
    private Mock<IRepository<Order>> _orderRepositoryMock;
    private OrderService _service;

    [TestInitialize]
    public void Init()
    {
        _orderRepositoryMock = new Mock<IRepository<Order>>();
        _orderNoteRepositoryMock = new Mock<IRepository<OrderNote>>();
        _mediatorMock = new Mock<IMediator>();
        _service = new OrderService(_orderRepositoryMock.Object, _orderNoteRepositoryMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task InsertOrder_AssignsNextOrderNumber()
    {
        _orderRepositoryMock.Setup(c => c.Table).Returns(new List<Order>().AsQueryable());
        _orderRepositoryMock.Setup(c => c.FirstOrDefaultAsync(It.IsAny<IQueryable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(41);

        var order = new Order();
        await _service.InsertOrder(order);

        Assert.AreEqual(42, order.OrderNumber);
        _orderRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<Order>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Order>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task InsertOrder_NoOrdersYet_AssignsFirstOrderNumber()
    {
        _orderRepositoryMock.Setup(c => c.Table).Returns(new List<Order>().AsQueryable());
        _orderRepositoryMock.Setup(c => c.FirstOrDefaultAsync(It.IsAny<IQueryable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var order = new Order();
        await _service.InsertOrder(order);

        Assert.AreEqual(1, order.OrderNumber);
    }

    [TestMethod]
    public async Task InsertOrder_OrderNumberTakenByAnotherCheckout_ReadsItAgainAndRetries()
    {
        _orderRepositoryMock.Setup(c => c.Table).Returns(new List<Order>().AsQueryable());
        _orderRepositoryMock.SetupSequence(c => c.FirstOrDefaultAsync(It.IsAny<IQueryable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(41)
            .ReturnsAsync(42);

        var order = new Order();
        _orderRepositoryMock.SetupSequence(c => c.InsertAsync(It.IsAny<Order>()))
            .Throws(new DuplicateKeyGrandException())
            .ReturnsAsync(order);

        await _service.InsertOrder(order);

        Assert.AreEqual(43, order.OrderNumber);
        _orderRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<Order>()), Times.Exactly(2));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Order>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task InsertOrder_OrderNumberTakenOnEveryAttempt_ThrowsAndDoesNotNotify()
    {
        _orderRepositoryMock.Setup(c => c.Table).Returns(new List<Order>().AsQueryable());
        _orderRepositoryMock.Setup(c => c.FirstOrDefaultAsync(It.IsAny<IQueryable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(41);
        _orderRepositoryMock.Setup(c => c.InsertAsync(It.IsAny<Order>()))
            .Throws(new DuplicateKeyGrandException());

        await Assert.ThrowsExactlyAsync<DuplicateKeyGrandException>(async () =>
            await _service.InsertOrder(new Order()));

        _orderRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<Order>()), Times.Exactly(5));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Order>>(), default), Times.Never);
    }

    [TestMethod]
    public async Task InsertOrder_NullArguments_ThrowException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.InsertOrder(null));
    }

    [TestMethod]
    public async Task UpdateOrder_InvokeExpectedMethods()
    {
        await _service.UpdateOrder(new Order());
        _orderRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Order>>(), default), Times.Once);
    }

    [TestMethod]
    public void UpdateOrder_NullArguments_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.UpdateOrder(null));
    }

    [TestMethod]
    public async Task InsertOrderNote_InvokeExpectedMethods()
    {
        await _service.InsertOrderNote(new OrderNote());
        _orderNoteRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<OrderNote>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<OrderNote>>(), default), Times.Once);
    }

    [TestMethod]
    public void InsertOrderNote_NullArguments_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.InsertOrderNote(null));
    }

    [TestMethod]
    public async Task DeleteOrderNote_InvokeExpectedMethods()
    {
        await _service.DeleteOrderNote(new OrderNote());
        _orderNoteRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<OrderNote>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<OrderNote>>(), default), Times.Once);
    }

    [TestMethod]
    public void DeleteOrderNote_NullArguments_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.DeleteOrderNote(null));
    }

    [TestMethod]
    public async Task CancelExpiredOrders_CancelledAfterTheFirstOrder_LeavesTheRestPending()
    {
        var orders = new List<Order> {
            NewExpiredOrder("1"),
            NewExpiredOrder("2")
        };
        _orderRepositoryMock.Setup(c => c.Table).Returns(orders.AsQueryable());
        using var cts = new CancellationTokenSource();
        //the order being cancelled is cancelled completely - the token is not passed into it
        _mediatorMock.Setup(c => c.Send(It.IsAny<CancelOrderCommand>(), CancellationToken.None))
            .Callback(() => cts.Cancel())
            .ReturnsAsync(true);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            _service.CancelExpiredOrders(DateTime.UtcNow, cts.Token));

        _mediatorMock.Verify(c => c.Send(It.Is<CancelOrderCommand>(x => x.Order.Id == "1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(c => c.Send(It.Is<CancelOrderCommand>(x => x.Order.Id == "2"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Order NewExpiredOrder(string id)
    {
        return new Order {
            Id = id,
            CreatedOnUtc = DateTime.UtcNow.AddDays(-10),
            PaymentStatusId = PaymentStatus.Pending,
            OrderStatusId = (int)OrderStatusSystem.Pending,
            ShippingStatusId = ShippingStatus.Pending
        };
    }
}
