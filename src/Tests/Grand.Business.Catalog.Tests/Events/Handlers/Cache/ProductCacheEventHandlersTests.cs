using Grand.Business.Catalog.Events.Handlers.Cache;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers.Cache;

[TestClass]
public class ProductCacheEventHandlersTests
{
    [TestMethod]
    public async Task ProductCategory_InsertedProductCategory_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<ProductCategory>(new ProductCategory());
        await new ProductCategoryCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductCategory_UpdatedProductCategory_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<ProductCategory>(new ProductCategory());
        await new ProductCategoryCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductCategory_DeletedProductCategory_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<ProductCategory>(new ProductCategory());
        await new ProductCategoryCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductCollection_InsertedProductCollection_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<ProductCollection>(new ProductCollection());
        await new ProductCollectionCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductCollection_UpdatedProductCollection_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<ProductCollection>(new ProductCollection());
        await new ProductCollectionCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductCollection_DeletedProductCollection_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<ProductCollection>(new ProductCollection());
        await new ProductCollectionCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductAttribute_InsertedProductAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<ProductAttribute>(new ProductAttribute());
        await new ProductAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductAttribute_UpdatedProductAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<ProductAttribute>(new ProductAttribute());
        await new ProductAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProductAttribute_DeletedProductAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<ProductAttribute>(new ProductAttribute());
        await new ProductAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SpecificationAttribute_InsertedSpecificationAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<SpecificationAttribute>(new SpecificationAttribute());
        await new SpecificationAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SpecificationAttribute_UpdatedSpecificationAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<SpecificationAttribute>(new SpecificationAttribute());
        await new SpecificationAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SpecificationAttribute_DeletedSpecificationAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<SpecificationAttribute>(new SpecificationAttribute());
        await new SpecificationAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SpecificationAttribute_DeletedSpecificationAttributeOption_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<SpecificationAttributeOption>(new SpecificationAttributeOption());
        await new SpecificationAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SpecificationAttribute_InsertedProductSpecificationAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<ProductSpecificationAttribute>(new ProductSpecificationAttribute());
        await new SpecificationAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SpecificationAttribute_UpdatedProductSpecificationAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<ProductSpecificationAttribute>(new ProductSpecificationAttribute());
        await new SpecificationAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SpecificationAttribute_DeletedProductSpecificationAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<ProductSpecificationAttribute>(new ProductSpecificationAttribute());
        await new SpecificationAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerGroupProduct_InsertedCustomerGroupProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<CustomerGroupProduct>(new CustomerGroupProduct { CustomerGroupId = "g1" });
        await new CustomerGroupProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, notification.Entity.CustomerGroupId), true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerGroupProduct_UpdatedCustomerGroupProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<CustomerGroupProduct>(new CustomerGroupProduct { CustomerGroupId = "g1" });
        await new CustomerGroupProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, notification.Entity.CustomerGroupId), true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CustomerGroupProduct_DeletedCustomerGroupProduct_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<CustomerGroupProduct>(new CustomerGroupProduct { CustomerGroupId = "g1" });
        await new CustomerGroupProductCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, notification.Entity.CustomerGroupId), true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
