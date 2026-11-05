using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers;

public class DeleteMeasureUnitOnProductEventHandler : INotificationHandler<EntityDeleted<MeasureUnit>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<Product> _repositoryProduct;

    public DeleteMeasureUnitOnProductEventHandler(IRepository<Product> repositoryProduct, ICacheBase cacheBase)
    {
        _repositoryProduct = repositoryProduct;
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<MeasureUnit> notification, CancellationToken cancellationToken)
    {
        await _repositoryProduct.UpdateManyAsync(x => x.UnitId == notification.Entity.Id,
            UpdateBuilder<Product>.Create().Set(x => x.UnitId, ""));

        //clear cache - here, after the write, so no request caches a product with the deleted unit again
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}