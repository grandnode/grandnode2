using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Tax;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers;

public class TaxCategoryDeletedEventHandler : INotificationHandler<EntityDeleted<TaxCategory>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<Product> _productRepository;

    public TaxCategoryDeletedEventHandler(IRepository<Product> productRepository, ICacheBase cacheBase)
    {
        _productRepository = productRepository;
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<TaxCategory> notification, CancellationToken cancellationToken)
    {
        await _productRepository.UpdateManyAsync(x => x.TaxCategoryId == notification.Entity.Id,
            UpdateBuilder<Product>.Create().Set(x => x.TaxCategoryId, ""));

        //clear cache - here, after the write, so no request caches a product with the deleted tax category again
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}