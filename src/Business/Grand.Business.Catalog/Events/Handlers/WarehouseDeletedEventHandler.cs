using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Utilities.Catalog;
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
    private readonly IResourceLock _resourceLock;
    private readonly StockLedger _stockLedger;

    public WarehouseDeletedEventHandler(IRepository<Product> productRepository, ICacheBase cacheBase,
        IResourceLock resourceLock, StockLedger stockLedger)
    {
        _productRepository = productRepository;
        _cacheBase = cacheBase;
        _resourceLock = resourceLock;
        _stockLedger = stockLedger;
    }

    public async Task Handle(EntityDeleted<Warehouse> notification, CancellationToken cancellationToken)
    {
        var warehouseId = notification.Entity.Id;
        var productIds = await _productRepository.ToListAsync(_productRepository.Table
            .Where(x => x.ProductWarehouseInventory.Any(w => w.WarehouseId == warehouseId))
            .Select(x => x.Id), cancellationToken);

        foreach (var productId in productIds)
            await RemoveWarehouseRow(productId, warehouseId);

        await _productRepository.UpdateManyAsync(x => x.WarehouseId == warehouseId,
            UpdateBuilder<Product>.Create().Set(x => x.WarehouseId, ""));

        //clear cache - here, after the write, so no request caches a product with the deleted warehouse again
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }

    private async Task RemoveWarehouseRow(string productId, string warehouseId)
    {
        await using var _ = await _resourceLock.Acquire([ProductStockLock.Key(productId)]);

        var product = await _productRepository.GetByIdAsync(productId);
        var row = product?.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
        if (row == null)
            return;

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStock || !product.UseMultipleWarehouses)
        {
            await _productRepository.RemoveCollectionFieldItem(productId, x => x.ProductWarehouseInventory,
                z => z.WarehouseId == warehouseId);
            return;
        }

        //the stock the warehouse held leaves the journal first, so the product's history keeps adding up
        if (row.StockQuantity != 0)
            await _stockLedger.Apply(product, [
                new StockChange([], warehouseId, -row.StockQuantity, false, InventoryMovementType.Adjustment,
                    new StockSource(nameof(Warehouse), warehouseId, Comment: "Warehouse deleted"))
            ]);

        product.ProductWarehouseInventory.Remove(row);
        await _stockLedger.RecomputeSums(product);
    }
}
