using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class CustomerGroupProductCacheEventHandler :
    INotificationHandler<EntityInserted<CustomerGroupProduct>>,
    INotificationHandler<EntityUpdated<CustomerGroupProduct>>,
    INotificationHandler<EntityDeleted<CustomerGroupProduct>>
{
    private readonly ICacheBase _cacheBase;

    public CustomerGroupProductCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CustomerGroupProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, notification.Entity.CustomerGroupId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);
    }

    public async Task Handle(EntityUpdated<CustomerGroupProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, notification.Entity.CustomerGroupId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);
    }

    public async Task Handle(EntityDeleted<CustomerGroupProduct> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, notification.Entity.CustomerGroupId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);
    }
}
