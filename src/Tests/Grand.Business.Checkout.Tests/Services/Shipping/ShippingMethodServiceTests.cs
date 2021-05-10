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
    public class ShippingMethodServiceTests
    {
        private Mock<IRepository<ShippingMethod>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<ICacheBase> _cacheMock;
        private IShippingMethodService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<ShippingMethod>>();
            _mediatorMock = new Mock<IMediator>();
            _cacheMock = new Mock<ICacheBase>();
            _service = new ShippingMethodService(_repositoryMock.Object, _mediatorMock.Object, _cacheMock.Object);
        }

        [TestMethod]
        public async Task InsertShippingMethod_InvokeExpectedMethods()
        {
            await _service.InsertShippingMethod(new ShippingMethod());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<ShippingMethod>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<ShippingMethod>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void InsertShippingMethod_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertShippingMethod(null));
        }

        [TestMethod]
        public async Task UpdateShippingMethod_InvokeExpectedMethods()
        {
            await _service.UpdateShippingMethod(new ShippingMethod());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<ShippingMethod>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<ShippingMethod>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void UpdateShippingMethod_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateShippingMethod(null));
        }

        [TestMethod]
        public async Task DeleteShippingMethod_InvokeExpectedMethods()
        {
            await _service.DeleteShippingMethod(new ShippingMethod());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<ShippingMethod>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<ShippingMethod>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void DeleteShippingMethod_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteShippingMethod(null));
        }
    }
}
