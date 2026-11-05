using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class MeasureCacheEventHandler :
    INotificationHandler<EntityInserted<MeasureDimension>>,
    INotificationHandler<EntityUpdated<MeasureDimension>>,
    INotificationHandler<EntityDeleted<MeasureDimension>>,
    INotificationHandler<EntityInserted<MeasureWeight>>,
    INotificationHandler<EntityUpdated<MeasureWeight>>,
    INotificationHandler<EntityDeleted<MeasureWeight>>,
    INotificationHandler<EntityInserted<MeasureUnit>>,
    INotificationHandler<EntityUpdated<MeasureUnit>>,
    INotificationHandler<EntityDeleted<MeasureUnit>>
{
    private readonly ICacheBase _cacheBase;

    public MeasureCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<MeasureDimension> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREDIMENSIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<MeasureDimension> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREDIMENSIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<MeasureDimension> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREDIMENSIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<MeasureWeight> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREWEIGHTS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<MeasureWeight> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREWEIGHTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<MeasureWeight> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREWEIGHTS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<MeasureUnit> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREUNITS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<MeasureUnit> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREUNITS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<MeasureUnit> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MEASUREUNITS_PATTERN_KEY);
    }
}
