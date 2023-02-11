﻿using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Events.Orders.Tests
{
    [TestClass()]
    public class MerchandiseReturnInsertEventHandlerTests
    {
        private MerchandiseReturnInsertEventHandler _merchandiseReturnInsertEventHandler;
        private Mock<IOrderService> _orderServiceMock;

        [TestInitialize()]
        public void Init()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _merchandiseReturnInsertEventHandler = new MerchandiseReturnInsertEventHandler(_orderServiceMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(new Order()));
            var notification = new Infrastructure.Events.EntityInserted<MerchandiseReturn>(new MerchandiseReturn());
            //Act
            await _merchandiseReturnInsertEventHandler.Handle(notification, CancellationToken.None);
            //Assert
            _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        }
    }
}