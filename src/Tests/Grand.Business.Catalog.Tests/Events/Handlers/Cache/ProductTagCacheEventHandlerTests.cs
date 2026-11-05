using Grand.Business.Catalog.Events.Handlers.Cache;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers.Cache;

[TestClass]
public class ProductTagCacheEventHandlerTests
{
    [TestMethod]
    public async Task ProductTag_InsertedProductTag_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<ProductTag>(new ProductTag());
        await new ProductTagCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductTag_UpdatedProductTag_ClearsItsCacheAndTheProductsCarryingItsName()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<ProductTag>(new ProductTag());
        await new ProductTagCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductTag_DeletedProductTag_ClearsItsCacheAndListingsButNotEveryProduct()
    {
        var cache = new Mock<ICacheBase>();

        //DetachProductTag publishes this event too, for every tag removed from a product
        var notification = new EntityDeleted<ProductTag>(new ProductTag());
        await new ProductTagCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
