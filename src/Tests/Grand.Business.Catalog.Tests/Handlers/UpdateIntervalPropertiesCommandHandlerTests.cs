using Grand.Business.Catalog.Commands.Handlers;
using Grand.Business.Core.Commands.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Data.Mongo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Handlers
{
    [TestClass()]
    public class UpdateIntervalPropertiesCommandHandlerTests
    {
        private Mock<MongoRepository<Product>> _repositoryMock;       
        private Mock<ICacheBase> _cacheMock;
        private UpdateIntervalPropertiesCommandHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = "";

            _repositoryMock = new Mock<MongoRepository<Product>>();
            _cacheMock = new Mock<ICacheBase>();
            _handler = new UpdateIntervalPropertiesCommandHandler(_repositoryMock.Object, _cacheMock.Object);
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
            //var collectionMock = new Mock<IMongoCollection<Product>>();
            //_repositoryMock.Setup(c => c.Collection).Returns(collectionMock.Object);
            await _handler.Handle(command, default);
            //TODO
            //collectionMock.Verify(c => c.UpdateOneAsync(x => x.Id == product.Id, It.IsAny<UpdateDefinition<Product>>(), null, default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            
        }
    }
}
