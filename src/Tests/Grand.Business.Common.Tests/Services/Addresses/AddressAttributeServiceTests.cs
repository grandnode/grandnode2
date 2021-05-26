using Grand.Business.Common.Services.Addresses;
using Grand.Domain.Common;
using Grand.Domain.Data.Mongo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Addresses
{
    [TestClass()]
    public class AddressAttributeServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<MongoRepository<AddressAttribute>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private AddressAttributeService _service;

        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = "";
            _cacheMock = new Mock<ICacheBase>();
            _repositoryMock = new Mock<MongoRepository<AddressAttribute>>();
            _mediatorMock = new Mock<IMediator>();
            _service = new AddressAttributeService(_cacheMock.Object, _repositoryMock.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task InsertAddressAttribute_ValidArgument_InvokeExpectedMethod()
        {
            await _service.InsertAddressAttribute(new AddressAttribute());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<AddressAttribute>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<AddressAttribute>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()));
        }

        [TestMethod()]
        public async Task DeleteAddressAttribute_ValidArgument_InvokeExpectedMethod()
        {
            await _service.DeleteAddressAttribute(new AddressAttribute());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<AddressAttribute>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<AddressAttribute>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()));
        }

        [TestMethod()]
        public async Task UpdateAddressAttribute_ValidArgument_InvokeExpectedMethod()
        {
            await _service.UpdateAddressAttribute(new AddressAttribute());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<AddressAttribute>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<AddressAttribute>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()));
        }

        [TestMethod()]
        public async Task InsertAddressAttributeValue_ValidArgument_InvokeExpectedMethod()
        {
            await _service.InsertAddressAttributeValue(new AddressAttributeValue() { AddressAttributeId = "id" });
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<AddressAttributeValue>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()));
        }

        [TestMethod()]
        public async Task DeleteAddressAttributeValue_ValidArgument_InvokeExpectedMethod()
        {
            await _service.DeleteAddressAttributeValue(new AddressAttributeValue() { AddressAttributeId = "id" });
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<AddressAttributeValue>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()));
        }

        [TestMethod()]
        public async Task UpdateAddressAttributeValue_ValidArgument_InvokeExpectedMethod()
        {
            await _service.UpdateAddressAttributeValue(new AddressAttributeValue() { AddressAttributeId = "id" });
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<AddressAttributeValue>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()));
        }
    }
}
