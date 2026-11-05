using Grand.Business.Marketing.Events.Cache;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Events.Cache;

[TestClass]
public class MarketingCacheEventHandlersTests
{
    [TestMethod]
    public async Task ContactAttribute_InsertedContactAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<ContactAttribute>(new ContactAttribute());
        await new ContactAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ContactAttribute_UpdatedContactAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<ContactAttribute>(new ContactAttribute());
        await new ContactAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ContactAttribute_DeletedContactAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<ContactAttribute>(new ContactAttribute());
        await new ContactAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerProduct_InsertedCustomerProductPrice_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<CustomerProductPrice>(new CustomerProductPrice { CustomerId = "c1", ProductId = "p1" });
        await new CustomerProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, notification.Entity.CustomerId, notification.Entity.ProductId), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerProduct_UpdatedCustomerProductPrice_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<CustomerProductPrice>(new CustomerProductPrice { CustomerId = "c1", ProductId = "p1" });
        await new CustomerProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, notification.Entity.CustomerId, notification.Entity.ProductId), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerProduct_DeletedCustomerProductPrice_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<CustomerProductPrice>(new CustomerProductPrice { CustomerId = "c1", ProductId = "p1" });
        await new CustomerProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, notification.Entity.CustomerId, notification.Entity.ProductId), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerProduct_InsertedCustomerProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<CustomerProduct>(new CustomerProduct { CustomerId = "c1" });
        await new CustomerProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, notification.Entity.CustomerId), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerProduct_UpdatedCustomerProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<CustomerProduct>(new CustomerProduct { CustomerId = "c1" });
        await new CustomerProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, notification.Entity.CustomerId), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerProduct_DeletedCustomerProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<CustomerProduct>(new CustomerProduct { CustomerId = "c1" });
        await new CustomerProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, notification.Entity.CustomerId), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerTagProduct_InsertedCustomerTagProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<CustomerTagProduct>(new CustomerTagProduct { CustomerTagId = "t1" });
        await new CustomerTagProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, notification.Entity.CustomerTagId), true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerTagProduct_UpdatedCustomerTagProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<CustomerTagProduct>(new CustomerTagProduct { CustomerTagId = "t1" });
        await new CustomerTagProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, notification.Entity.CustomerTagId), true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerTagProduct_DeletedCustomerTagProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<CustomerTagProduct>(new CustomerTagProduct { CustomerTagId = "t1" });
        await new CustomerTagProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, notification.Entity.CustomerTagId), true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
