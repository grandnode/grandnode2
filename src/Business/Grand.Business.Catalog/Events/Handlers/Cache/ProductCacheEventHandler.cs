using Grand.Business.Core.Events.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

/// <summary>
///     Clears the cached products. An update is announced by ProductChangedEvent, which says how far it reaches,
///     so EntityUpdated&lt;Product&gt; is not handled here.
/// </summary>
public class ProductCacheEventHandler :
    INotificationHandler<EntityInserted<Product>>,
    INotificationHandler<EntityDeleted<Product>>,
    INotificationHandler<ProductChangedEvent>
{
    private readonly ICacheBase _cacheBase;

    public ProductCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Product> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Product> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }

    public async Task Handle(ProductChangedEvent notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, notification.ProductId));

        if (!notification.ListsChanged)
            return;

        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_PATTERN);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_HOMEPAGE_PATTERN);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);
    }
}
