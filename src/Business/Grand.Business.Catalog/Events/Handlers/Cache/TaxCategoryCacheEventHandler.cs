using Grand.Domain.Tax;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class TaxCategoryCacheEventHandler :
    INotificationHandler<EntityInserted<TaxCategory>>,
    INotificationHandler<EntityUpdated<TaxCategory>>,
    INotificationHandler<EntityDeleted<TaxCategory>>
{
    private readonly ICacheBase _cacheBase;

    public TaxCategoryCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<TaxCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<TaxCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<TaxCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);
    }
}
