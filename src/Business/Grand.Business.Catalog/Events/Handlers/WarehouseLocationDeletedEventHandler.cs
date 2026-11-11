using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers;

/// <summary>
///     The batches and serial numbers kept in a deleted location are left without one
/// </summary>
public class WarehouseLocationDeletedEventHandler(
    IRepository<ProductBatch> batchRepository,
    IRepository<ProductSerialNumber> serialNumberRepository)
    : INotificationHandler<EntityDeleted<WarehouseLocation>>
{
    public async Task Handle(EntityDeleted<WarehouseLocation> notification, CancellationToken cancellationToken)
    {
        var locationId = notification.Entity.Id;
        await batchRepository.UpdateManyAsync(x => x.LocationId == locationId,
            UpdateBuilder<ProductBatch>.Create().Set(x => x.LocationId, null));
        await serialNumberRepository.UpdateManyAsync(x => x.LocationId == locationId,
            UpdateBuilder<ProductSerialNumber>.Create().Set(x => x.LocationId, null));
    }
}
