using Grand.Data;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Shipping;

/// <summary>
///     A deleted warehouse takes its locations with it, in one write; the units that named one keep an id no screen
///     finds any more
/// </summary>
public class DeleteWarehouseLocationsEventHandler(IRepository<WarehouseLocation> locationRepository)
    : INotificationHandler<EntityDeleted<Warehouse>>
{
    public Task Handle(EntityDeleted<Warehouse> notification, CancellationToken cancellationToken)
    {
        var warehouseId = notification.Entity.Id;
        return locationRepository.DeleteManyAsync(x => x.WarehouseId == warehouseId);
    }
}
