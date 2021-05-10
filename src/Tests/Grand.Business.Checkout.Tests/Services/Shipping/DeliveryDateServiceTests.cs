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
    public class DeliveryDateServiceTests
    {
        private Mock<IRepository<DeliveryDate>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<ICacheBase> _cacheMock;
        private IDeliveryDateService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<DeliveryDate>>();
            _mediatorMock = new Mock<IMediator>();
            _cacheMock = new Mock<ICacheBase>();
            _service = new DeliveryDateService(_repositoryMock.Object, _mediatorMock.Object, _cacheMock.Object);
        }

        [TestMethod]
        public async Task InsertDeliveryDate_InvokeExpectedMethods()
        {
            await _service.InsertDeliveryDate(new DeliveryDate());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<DeliveryDate>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<DeliveryDate>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void InsertDeliveryDate_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertDeliveryDate(null));
        }

        [TestMethod]
        public async Task UpdateDeliveryDate_InvokeExpectedMethods()
        {
            await _service.UpdateDeliveryDate(new DeliveryDate());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<DeliveryDate>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<DeliveryDate>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void UpdateDeliveryDate_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateDeliveryDate(null));
        }

        [TestMethod]
        public async Task DeleteDeliveryDate_InvokeExpectedMethods()
        {
            await _service.DeleteDeliveryDate(new DeliveryDate());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<DeliveryDate>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<DeliveryDate>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void DeleteDeliveryDate_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteDeliveryDate(null));
        }
    }
}
