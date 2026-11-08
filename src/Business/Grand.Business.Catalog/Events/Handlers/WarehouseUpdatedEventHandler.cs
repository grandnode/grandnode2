using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers;

public class WarehouseUpdatedEventHandler : INotificationHandler<EntityUpdated<Warehouse>>
{
    private readonly IRepository<ProductBatch> _batchRepository;
    private readonly IRepository<ProductSerialNumber> _serialNumberRepository;

    public WarehouseUpdatedEventHandler(IRepository<ProductBatch> batchRepository,
        IRepository<ProductSerialNumber> serialNumberRepository)
    {
        _batchRepository = batchRepository;
        _serialNumberRepository = serialNumberRepository;
    }

    public async Task Handle(EntityUpdated<Warehouse> notification, CancellationToken cancellationToken)
    {
        //a warehouse moved to another store takes its batches and serial numbers with it, so the store sees them
        var warehouseId = notification.Entity.Id;
        var storeId = notification.Entity.StoreId ?? "";

        await _batchRepository.UpdateManyAsync(x => x.WarehouseId == warehouseId && x.StoreId != storeId,
            UpdateBuilder<ProductBatch>.Create().Set(x => x.StoreId, storeId));
        await _serialNumberRepository.UpdateManyAsync(x => x.WarehouseId == warehouseId && x.StoreId != storeId,
            UpdateBuilder<ProductSerialNumber>.Create().Set(x => x.StoreId, storeId));
    }
}
