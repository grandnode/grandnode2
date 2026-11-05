using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class ProductLayoutCacheEventHandler :
    INotificationHandler<EntityInserted<ProductLayout>>,
    INotificationHandler<EntityUpdated<ProductLayout>>,
    INotificationHandler<EntityDeleted<ProductLayout>>
{
    private readonly ICacheBase _cacheBase;

    public ProductLayoutCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ProductLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCT_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ProductLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCT_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ProductLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCT_LAYOUT_PATTERN_KEY);
    }
}
