﻿using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Events.Orders.Tests
{
    [TestClass()]
    public class OrderDeletedEventHandlerTests
    {
        private OrderDeletedEventHandler _orderDeletedEventHandler;
        private Mock<IRepository<ProductAlsoPurchased>> _productAlsoPurchasedRepository;

        [TestInitialize()]
        public void Init()
        {
            _productAlsoPurchasedRepository = new Mock<IRepository<ProductAlsoPurchased>>();
            _orderDeletedEventHandler = new OrderDeletedEventHandler(_productAlsoPurchasedRepository.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var order = new Domain.Orders.Order();
            var notification = new OrderDeletedEvent(order);
            //Act
            await _orderDeletedEventHandler.Handle(notification, CancellationToken.None);
            //Assert
            _productAlsoPurchasedRepository.Verify(c => c.DeleteManyAsync(x => x.OrderId == notification.Order.Id), Times.Once);
        }
    }
}