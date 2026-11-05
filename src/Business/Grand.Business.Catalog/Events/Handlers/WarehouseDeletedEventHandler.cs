using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers;

public class WarehouseDeletedEventHandler : INotificationHandler<EntityDeleted<Warehouse>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<Product> _productRepository;

    public WarehouseDeletedEventHandler(IRepository<Product> productRepository, ICacheBase cacheBase)
    {
        _productRepository = productRepository;
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<Warehouse> notification, CancellationToken cancellationToken)
    {
        await _productRepository.RemoveCollectionFieldItem(string.Empty, x => x.ProductWarehouseInventory,
            z => z.WarehouseId == notification.Entity.Id);

        await _productRepository.UpdateManyAsync(x => x.WarehouseId == notification.Entity.Id,
            UpdateBuilder<Product>.Create().Set(x => x.WarehouseId, ""));

        //clear cache - here, after the write, so no request caches a product with the deleted warehouse again
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}