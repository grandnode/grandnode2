using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Services.Shipping;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
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

namespace Grand.Business.Checkout.Tests.Services.Shipping
{
    [TestClass]
    public class WarehouseServiceTests
    {
        private Mock<IRepository<Warehouse>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<ICacheBase> _cacheMock;
        private IWarehouseService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<Warehouse>>();
            _mediatorMock = new Mock<IMediator>();
            _cacheMock = new Mock<ICacheBase>();
            _service = new WarehouseService(_repositoryMock.Object,_mediatorMock.Object,_cacheMock.Object);
        }


        [TestMethod]
        public async Task InsertWarehouse_InvokeExpectedMethods()
        {
            await _service.InsertWarehouse(new Warehouse());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Warehouse>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Warehouse>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void InsertWarehouse_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertWarehouse(null));
        }

        [TestMethod]
        public async Task UpdateWarehouse_InvokeExpectedMethods()
        {
            await _service.UpdateWarehouse(new Warehouse());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Warehouse>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Warehouse>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void UpdateWarehouse_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateWarehouse(null));
        }

        [TestMethod]
        public async Task DeleteWarehouse_InvokeExpectedMethods()
        {
            await _service.DeleteWarehouse(new Warehouse());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Warehouse>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Warehouse>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void DeleteWarehouse_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteWarehouse(null));
        }
    }
}
