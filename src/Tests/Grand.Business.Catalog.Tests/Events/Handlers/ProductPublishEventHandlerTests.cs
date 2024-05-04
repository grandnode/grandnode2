using Grand.Business.Catalog.Events.Handlers;
using Grand.Business.Core.Events.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class ProductPublishEventHandlerTests
{
    private Mock<ICacheBase> _casheManagerMock;

    private ProductPublishEventHandler handlerPublish;
    private ProductUnPublishEventHandler handlerUnPublish;

    [TestInitialize]
    public void Init()
    {
        _casheManagerMock = new Mock<ICacheBase>();
        handlerPublish = new ProductPublishEventHandler(_casheManagerMock.Object);
        handlerUnPublish = new ProductUnPublishEventHandler(_casheManagerMock.Object);
    }

    [TestMethod]
    public async Task Handle_Publish()
    {
        //Act
        await handlerPublish.Handle(new ProductPublishEvent(new Product { ShowOnHomePage = true }),
            CancellationToken.None);
        _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
    }

    [TestMethod]
    public async Task Handle_UnPublish()
    {
        //Act
        await handlerUnPublish.Handle(new ProductUnPublishEvent(new Product { ShowOnHomePage = true }),
            CancellationToken.None);
        _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
    }
}