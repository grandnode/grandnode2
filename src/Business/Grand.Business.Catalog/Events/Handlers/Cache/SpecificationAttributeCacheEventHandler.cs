using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class SpecificationAttributeCacheEventHandler :
    INotificationHandler<EntityInserted<SpecificationAttribute>>,
    INotificationHandler<EntityUpdated<SpecificationAttribute>>,
    INotificationHandler<EntityDeleted<SpecificationAttribute>>,
    INotificationHandler<EntityDeleted<SpecificationAttributeOption>>,
    INotificationHandler<EntityInserted<ProductSpecificationAttribute>>,
    INotificationHandler<EntityUpdated<ProductSpecificationAttribute>>,
    INotificationHandler<EntityDeleted<ProductSpecificationAttribute>>
{
    private readonly ICacheBase _cacheBase;

    public SpecificationAttributeCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<SpecificationAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<SpecificationAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<SpecificationAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<SpecificationAttributeOption> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SPECIFICATION_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<ProductSpecificationAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ProductSpecificationAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ProductSpecificationAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }
}
