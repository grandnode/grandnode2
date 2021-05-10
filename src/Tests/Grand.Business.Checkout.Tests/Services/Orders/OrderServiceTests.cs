using Grand.Business.Checkout.Services.Orders;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Services.Orders
{
    [TestClass]
    public class OrderServiceTests
    {
        private Mock<IRepository<Order>> _orderRepositoryMock;
        private Mock<IRepository<OrderNote>> _orderNoteRepositoryMock;
        private Mock<IMediator> _mediatorMock;
        private OrderService _service;

        [TestInitialize]
        public void Init()
        {
            _orderRepositoryMock = new Mock<IRepository<Order>>();
            _orderNoteRepositoryMock = new Mock<IRepository<OrderNote>>();
            _mediatorMock = new Mock<IMediator>();
            _service = new OrderService(_orderRepositoryMock.Object,_orderNoteRepositoryMock.Object,_mediatorMock.Object);
        }

        [TestMethod]
        public async Task UpdateOrder_InvokeExpectedMethods()
        {
            await _service.UpdateOrder(new Order());
            _orderRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Order>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Order>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void UpdateOrder_NullArguments_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateOrder(null));
        }

        [TestMethod]
        public async Task InsertOrderNote_InvokeExpectedMethods()
        {
            await _service.InsertOrderNote(new OrderNote());
            _orderNoteRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<OrderNote>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<OrderNote>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void InsertOrderNote_NullArguments_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertOrderNote(null));
        }

        [TestMethod]
        public async Task DeleteOrderNote_InvokeExpectedMethods()
        {
            await _service.DeleteOrderNote(new OrderNote());
            _orderNoteRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<OrderNote>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<OrderNote>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void DeleteOrderNote_NullArguments_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteOrderNote(null));
        }
    }
}
