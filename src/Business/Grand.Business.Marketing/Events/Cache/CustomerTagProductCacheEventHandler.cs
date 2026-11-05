using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Marketing.Events.Cache;

public class CustomerTagProductCacheEventHandler :
    INotificationHandler<EntityInserted<CustomerTagProduct>>,
    INotificationHandler<EntityUpdated<CustomerTagProduct>>,
    INotificationHandler<EntityDeleted<CustomerTagProduct>>
{
    private readonly ICacheBase _cacheBase;

    public CustomerTagProductCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CustomerTagProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, notification.Entity.CustomerTagId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);
    }

    public async Task Handle(EntityUpdated<CustomerTagProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, notification.Entity.CustomerTagId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);
    }

    public async Task Handle(EntityDeleted<CustomerTagProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, notification.Entity.CustomerTagId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);
    }
}
