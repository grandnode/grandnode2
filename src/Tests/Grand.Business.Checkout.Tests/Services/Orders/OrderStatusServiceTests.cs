using Grand.Business.Checkout.Services.Orders;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
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
    public class OrderStatusServiceTests
    {
        private Mock<IRepository<OrderStatus>> _repositoryMock;
        private Mock<ICacheBase> _cacheMock;
        private Mock<IMediator> _mediatorMock;
        private OrderStatusService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<OrderStatus>>();
            _cacheMock = new Mock<ICacheBase>();
            _mediatorMock = new Mock<IMediator>();
            _service = new OrderStatusService(_repositoryMock.Object, _cacheMock.Object, _mediatorMock.Object);
        }

        [TestMethod]
        public async Task Insert_InvokeExpectedMethods()
        {
            await _service.Insert(new OrderStatus());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<OrderStatus>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<OrderStatus>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void Insert_NullArguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.Insert(null));
        }

        [TestMethod]
        public async Task Update_InvokeExpectedMethods()
        {
            await _service.Update(new OrderStatus());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<OrderStatus>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<OrderStatus>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void Update_NullArguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.Update(null));
        }

        [TestMethod]
        public async Task Delete_InvokeExpectedMethods()
        {
            await _service.Delete(new OrderStatus());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<OrderStatus>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<OrderStatus>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void Delete_NullArguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.Delete(null));
        }
    }
}
