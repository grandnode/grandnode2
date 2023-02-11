﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Checkout.Events.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Moq;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Events.Orders.Tests
{
    [TestClass()]
    public class MerchandiseReturnDeletedEventHandlerTests
    {
        private MerchandiseReturnDeletedEventHandler _merchandiseReturnDeletedEventHandler;
        private Mock<IOrderService> _orderServiceMock;

        [TestInitialize()]
        public void Init()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _merchandiseReturnDeletedEventHandler = new MerchandiseReturnDeletedEventHandler(_orderServiceMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(new Order()));
            var notification  = new Infrastructure.Events.EntityDeleted<MerchandiseReturn>(new MerchandiseReturn());
            //Act
            await _merchandiseReturnDeletedEventHandler.Handle(notification, CancellationToken.None);
            //Assert
            _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        }
    }
}