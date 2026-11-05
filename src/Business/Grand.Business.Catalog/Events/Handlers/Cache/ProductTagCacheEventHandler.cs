using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

/// <summary>
///     Clears the cached product tags. AttachProductTag and DetachProductTag publish EntityInserted and
///     EntityDeleted for a tag that stays, so those two clear only the tags and the listings; the products a deleted
///     tag is removed from are cleared through ProductChangedEvent.
/// </summary>
public class ProductTagCacheEventHandler :
    INotificationHandler<EntityInserted<ProductTag>>,
    INotificationHandler<EntityUpdated<ProductTag>>,
    INotificationHandler<EntityDeleted<ProductTag>>
{
    private readonly ICacheBase _cacheBase;

    public ProductTagCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ProductTag> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ProductTag> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);
        //an update renames the tag on every product carrying it
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ProductTag> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }
}
