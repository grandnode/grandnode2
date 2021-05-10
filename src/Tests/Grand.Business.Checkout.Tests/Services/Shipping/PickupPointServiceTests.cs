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
    public class PickupPointServiceTests
    {
        private Mock<IRepository<PickupPoint>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<ICacheBase> _cacheMock;
        private IPickupPointService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<PickupPoint>>();
            _mediatorMock = new Mock<IMediator>();
            _cacheMock = new Mock<ICacheBase>();
            _service = new PickupPointService(_repositoryMock.Object,_mediatorMock.Object,_cacheMock.Object);
        }

        [TestMethod]
        public async Task InsertPickupPoint_InvokeExpectedMethods()
        {
            await _service.InsertPickupPoint(new PickupPoint());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<PickupPoint>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<PickupPoint>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void InsertPickupPoint_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertPickupPoint(null));
        }

        [TestMethod]
        public async Task UpdatePickupPoint_InvokeExpectedMethods()
        {
            await _service.UpdatePickupPoint(new PickupPoint());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<PickupPoint>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<PickupPoint>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void UpdatePickupPoint_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdatePickupPoint(null));
        }

        [TestMethod]
        public async Task DeletePickupPoint_InvokeExpectedMethods()
        {
            await _service.DeletePickupPoint(new PickupPoint());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<PickupPoint>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeast(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<PickupPoint>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void DeletePickupPoint_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeletePickupPoint(null));
        }
    }
}
