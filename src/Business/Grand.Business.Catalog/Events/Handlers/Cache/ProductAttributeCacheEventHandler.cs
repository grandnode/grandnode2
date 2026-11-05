using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class ProductAttributeCacheEventHandler :
    INotificationHandler<EntityInserted<ProductAttribute>>,
    INotificationHandler<EntityUpdated<ProductAttribute>>,
    INotificationHandler<EntityDeleted<ProductAttribute>>
{
    private readonly ICacheBase _cacheBase;

    public ProductAttributeCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ProductAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ProductAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ProductAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY);
    }
}
