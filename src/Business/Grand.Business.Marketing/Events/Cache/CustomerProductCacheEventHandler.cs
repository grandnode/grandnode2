using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Marketing.Events.Cache;

public class CustomerProductCacheEventHandler :
    INotificationHandler<EntityInserted<CustomerProductPrice>>,
    INotificationHandler<EntityUpdated<CustomerProductPrice>>,
    INotificationHandler<EntityDeleted<CustomerProductPrice>>,
    INotificationHandler<EntityInserted<CustomerProduct>>,
    INotificationHandler<EntityUpdated<CustomerProduct>>,
    INotificationHandler<EntityDeleted<CustomerProduct>>
{
    private readonly ICacheBase _cacheBase;

    public CustomerProductCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CustomerProductPrice> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, notification.Entity.CustomerId, notification.Entity.ProductId));
    }

    public async Task Handle(EntityUpdated<CustomerProductPrice> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, notification.Entity.CustomerId, notification.Entity.ProductId));
    }

    public async Task Handle(EntityDeleted<CustomerProductPrice> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, notification.Entity.CustomerId, notification.Entity.ProductId));
    }

    public async Task Handle(EntityInserted<CustomerProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, notification.Entity.CustomerId));
    }

    public async Task Handle(EntityUpdated<CustomerProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, notification.Entity.CustomerId));
    }

    public async Task Handle(EntityDeleted<CustomerProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, notification.Entity.CustomerId));
    }
}
