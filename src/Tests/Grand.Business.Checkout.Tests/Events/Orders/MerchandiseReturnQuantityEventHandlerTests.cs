using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class MerchandiseReturnQuantityEventHandlerTests
{
    private MerchandiseReturnQuantityEventHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Order _order;
    private Mock<IRepository<Order>> _orderRepositoryMock;
    private Mock<IOrderService> _orderServiceMock;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1" };
        _order.OrderItems.Add(new OrderItem { Id = "a", Quantity = 2 });
        _orderServiceMock = new Mock<IOrderService>();
        _orderServiceMock.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order);
        _orderRepositoryMock = new Mock<IRepository<Order>>();
        _mediatorMock = new Mock<IMediator>();
        _handler = new MerchandiseReturnQuantityEventHandler(_orderServiceMock.Object, _orderRepositoryMock.Object,
            _mediatorMock.Object);
    }

    //only the order item is written, never the whole order
    private void VerifyOrderItemWritten(Times times)
    {
        _orderRepositoryMock.Verify(x => x.UpdateCollectionFieldItem(It.IsAny<string>(),
            It.IsAny<Expression<Func<Order, IEnumerable<OrderItem>>>>(),
            It.IsAny<Expression<Func<OrderItem, bool>>>(), It.Is<OrderItem>(i => i.Id == "a")), times);
        _orderServiceMock.Verify(x => x.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    //the returns stored for the order, as the event handler finds them after the write
    private void SetupStoredReturns(params MerchandiseReturn[] merchandiseReturns)
    {
        _mediatorMock.Setup(x => x.Send(It.Is<GetMerchandiseReturnQuery>(q => q.OrderId == "o1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<MerchandiseReturn>(merchandiseReturns, 0, int.MaxValue));
    }

    private static MerchandiseReturn Return(MerchandiseReturnStatus status, int quantity)
    {
        var merchandiseReturn = new MerchandiseReturn { OrderId = "o1", MerchandiseReturnStatus = status };
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = quantity });
        return merchandiseReturn;
    }

    [TestMethod]
    public async Task Handle_ReturnInserted_TakesItsQuantity()
    {
        //Arrange
        var merchandiseReturn = Return(MerchandiseReturnStatus.Pending, 2);
        SetupStoredReturns(merchandiseReturn);

        //Act
        await _handler.Handle(new EntityInserted<MerchandiseReturn>(merchandiseReturn), CancellationToken.None);

        //Assert
        Assert.AreEqual(2, _order.OrderItems.Single().ReturnQty);
        VerifyOrderItemWritten(Times.Once());
    }

    [TestMethod]
    public async Task Handle_ReturnRejected_GivesItsQuantityBack()
    {
        //Arrange
        _order.OrderItems.Single().ReturnQty = 2;
        var merchandiseReturn = Return(MerchandiseReturnStatus.Rejected, 2);
        SetupStoredReturns(merchandiseReturn);

        //Act
        await _handler.Handle(new EntityUpdated<MerchandiseReturn>(merchandiseReturn), CancellationToken.None);

        //Assert
        Assert.AreEqual(0, _order.OrderItems.Single().ReturnQty);
        VerifyOrderItemWritten(Times.Once());
    }

    [TestMethod]
    public async Task Handle_CancelledReturnAcceptedAgain_TakesItsQuantityAgain()
    {
        //Arrange
        var merchandiseReturn = Return(MerchandiseReturnStatus.Accepted, 2);
        SetupStoredReturns(merchandiseReturn);

        //Act
        await _handler.Handle(new EntityUpdated<MerchandiseReturn>(merchandiseReturn), CancellationToken.None);

        //Assert
        Assert.AreEqual(2, _order.OrderItems.Single().ReturnQty);
    }

    [TestMethod]
    public async Task Handle_ReturnUpdatedWithoutQuantityChange_LeavesTheOrderAlone()
    {
        //Arrange - e.g. a staff note was edited
        _order.OrderItems.Single().ReturnQty = 2;
        var merchandiseReturn = Return(MerchandiseReturnStatus.Accepted, 2);
        SetupStoredReturns(merchandiseReturn);

        //Act
        await _handler.Handle(new EntityUpdated<MerchandiseReturn>(merchandiseReturn), CancellationToken.None);

        //Assert
        VerifyOrderItemWritten(Times.Never());
    }

    [TestMethod]
    public async Task Handle_ReturnDeleted_GivesItsQuantityBack()
    {
        //Arrange - the deleted return is no longer stored
        _order.OrderItems.Single().ReturnQty = 2;
        SetupStoredReturns();

        //Act
        await _handler.Handle(new EntityDeleted<MerchandiseReturn>(Return(MerchandiseReturnStatus.Pending, 2)),
            CancellationToken.None);

        //Assert
        Assert.AreEqual(0, _order.OrderItems.Single().ReturnQty);
        VerifyOrderItemWritten(Times.Once());
    }

    [TestMethod]
    public async Task Handle_RejectedReturnDeleted_KeepsTheReturnedQuantity()
    {
        //Arrange - an accepted return took 2; the rejected one, now deleted, never took anything
        _order.OrderItems.Single().ReturnQty = 2;
        SetupStoredReturns(Return(MerchandiseReturnStatus.Accepted, 2));

        //Act
        await _handler.Handle(new EntityDeleted<MerchandiseReturn>(Return(MerchandiseReturnStatus.Rejected, 1)),
            CancellationToken.None);

        //Assert
        Assert.AreEqual(2, _order.OrderItems.Single().ReturnQty);
    }

    [TestMethod]
    public async Task Handle_OrderNotFound_DoesNothing()
    {
        //Arrange
        var merchandiseReturn = Return(MerchandiseReturnStatus.Pending, 2);
        merchandiseReturn.OrderId = "missing";

        //Act
        await _handler.Handle(new EntityInserted<MerchandiseReturn>(merchandiseReturn), CancellationToken.None);

        //Assert
        VerifyOrderItemWritten(Times.Never());
    }
}
