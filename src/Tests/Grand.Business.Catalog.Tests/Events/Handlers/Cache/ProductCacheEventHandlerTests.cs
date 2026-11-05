using Grand.Business.Catalog.Events.Handlers.Cache;
using Grand.Business.Core.Events.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers.Cache;

[TestClass]
public class ProductCacheEventHandlerTests
{
    private Mock<ICacheBase> _cacheMock;
    private ProductCacheEventHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _handler = new ProductCacheEventHandler(_cacheMock.Object);
    }

    [TestMethod]
    public async Task InsertedProduct_ClearsEveryProductKey()
    {
        await _handler.Handle(new EntityInserted<Product>(new Product()), CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        _cacheMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task DeletedProduct_ClearsEveryProductKey()
    {
        await _handler.Handle(new EntityDeleted<Product>(new Product()), CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        _cacheMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductChanged_ClearsOnlyThatProduct()
    {
        await _handler.Handle(new ProductChangedEvent("p1"), CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, "p1"), true), Times.Once);
        _cacheMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductChanged_WithListsChanged_ClearsTheProductAndTheListsShowingIt()
    {
        await _handler.Handle(new ProductChangedEvent("p1", listsChanged: true), CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, "p1"), true), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_PATTERN, true), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN, true), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_HOMEPAGE_PATTERN, true), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN, true), Times.Once);
        _cacheMock.VerifyNoOtherCalls();
    }
}
