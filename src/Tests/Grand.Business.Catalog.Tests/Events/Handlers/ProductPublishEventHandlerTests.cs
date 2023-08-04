using Grand.Business.Catalog.Events.Handlers;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers
{
    [TestClass()]
    public class ProductPublishEventHandlerTests
    {

        ProductPublishEventHandler handlerPublish;
        ProductUnPublishEventHandler handlerUnPublish;
        private Mock<ICacheBase> _casheManagerMock;

        [TestInitialize()]
        public void Init()
        {
            _casheManagerMock = new Mock<ICacheBase>();
            handlerPublish = new ProductPublishEventHandler(_casheManagerMock.Object);
            handlerUnPublish = new ProductUnPublishEventHandler(_casheManagerMock.Object);
        }

        [TestMethod()]
        public async Task Handle_Publish()
        {
            //Act
            await handlerPublish.Handle(new Core.Events.Catalog.ProductPublishEvent(new Domain.Catalog.Product() { ShowOnHomePage = true }), CancellationToken.None);
            _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));

        }
        [TestMethod()]
        public async Task Handle_UnPublish()
        {
            //Act
            await handlerUnPublish.Handle(new Core.Events.Catalog.ProductUnPublishEvent(new Domain.Catalog.Product() { ShowOnHomePage = true }), CancellationToken.None);
            _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
        }
    }
}