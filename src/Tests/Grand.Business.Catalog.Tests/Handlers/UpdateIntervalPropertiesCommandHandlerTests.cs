using Grand.Business.Catalog.Commands.Handlers;
using Grand.Business.Catalog.Commands.Models;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Tests.Handlers
{
    [TestClass()]
    public class UpdateIntervalPropertiesCommandHandlerTests
    {
        private Mock<IRepository<Product>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<ICacheBase> _cacheMock;
        private UpdateIntervalPropertiesCommandHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<Product>>();
            _mediatorMock = new Mock<IMediator>();
            _cacheMock = new Mock<ICacheBase>();
            _handler = new UpdateIntervalPropertiesCommandHandler(_repositoryMock.Object, _mediatorMock.Object, _cacheMock.Object);
        }

        [TestMethod()]
        public void Handle_NullProduct_ThrowError()
        {
            var command = new UpdateIntervalPropertiesCommand() { Product = null };
            Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _handler.Handle(command, default));
        }

        [TestMethod()]
        public async Task Handle_ValidArgument_UpdateClearCachAndPublishEvent()
        {
            var product = new Product();
            var command = new UpdateIntervalPropertiesCommand() { Product = product };
            var collectionMock = new Mock<IMongoCollection<Product>>();
            _repositoryMock.Setup(c => c.Collection).Returns(collectionMock.Object);
            await _handler.Handle(command, default);
            //TODO
            //collectionMock.Verify(c => c.UpdateOneAsync(x => x.Id == product.Id, It.IsAny<UpdateDefinition<Product>>(), null, default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Product>>(), default(CancellationToken)), Times.Once);
        }
    }
}
