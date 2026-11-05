using Grand.Business.Customers.Events.Cache;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Events.Cache;

[TestClass]
public class CustomersCacheEventHandlersTests
{
    [TestMethod]
    public async Task CustomerAttribute_InsertedCustomerAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<CustomerAttribute>(new CustomerAttribute());
        await new CustomerAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerAttribute_UpdatedCustomerAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<CustomerAttribute>(new CustomerAttribute());
        await new CustomerAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerAttribute_DeletedCustomerAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<CustomerAttribute>(new CustomerAttribute());
        await new CustomerAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerAttribute_InsertedCustomerAttributeValue_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<CustomerAttributeValue>(new CustomerAttributeValue());
        await new CustomerAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerAttribute_UpdatedCustomerAttributeValue_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<CustomerAttributeValue>(new CustomerAttributeValue());
        await new CustomerAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerAttribute_DeletedCustomerAttributeValue_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<CustomerAttributeValue>(new CustomerAttributeValue());
        await new CustomerAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SalesEmployee_InsertedSalesEmployee_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<SalesEmployee>(new SalesEmployee());
        await new SalesEmployeeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SalesEmployee_UpdatedSalesEmployee_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<SalesEmployee>(new SalesEmployee());
        await new SalesEmployeeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SalesEmployee_DeletedSalesEmployee_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<SalesEmployee>(new SalesEmployee());
        await new SalesEmployeeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Vendor_UpdatedVendor_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<Vendor>(new Vendor { Id = "v1" });
        await new VendorCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.VENDOR_BY_ID_KEY, notification.Entity.Id), true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
