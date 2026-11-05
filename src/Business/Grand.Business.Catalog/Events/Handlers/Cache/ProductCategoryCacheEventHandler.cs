using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class ProductCategoryCacheEventHandler :
    INotificationHandler<EntityInserted<ProductCategory>>,
    INotificationHandler<EntityUpdated<ProductCategory>>,
    INotificationHandler<EntityDeleted<ProductCategory>>
{
    private readonly ICacheBase _cacheBase;

    public ProductCategoryCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ProductCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ProductCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ProductCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }
}
