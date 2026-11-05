using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers;

public class DeliveryDateDeletedEventHandler : INotificationHandler<EntityDeleted<DeliveryDate>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<Product> _productRepository;

    public DeliveryDateDeletedEventHandler(IRepository<Product> productRepository, ICacheBase cacheBase)
    {
        _productRepository = productRepository;
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<DeliveryDate> notification, CancellationToken cancellationToken)
    {
        await _productRepository.UpdateManyAsync(x => x.DeliveryDateId == notification.Entity.Id,
            UpdateBuilder<Product>.Create().Set(x => x.DeliveryDateId, ""));

        //clear cache - here, after the write, so no request caches a product with the deleted delivery date again
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}