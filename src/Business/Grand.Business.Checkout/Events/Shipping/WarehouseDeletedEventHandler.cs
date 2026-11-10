using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Shipping;

/// <summary>
///     A deleted warehouse takes its locations with it
/// </summary>
public class WarehouseDeletedEventHandler(IWarehouseLocationService locationService)
    : INotificationHandler<EntityDeleted<Warehouse>>
{
    public async Task Handle(EntityDeleted<Warehouse> notification, CancellationToken cancellationToken)
    {
        //one at a time, so the units kept in each are left without it
        foreach (var location in await locationService.GetLocations([notification.Entity.Id]))
            await locationService.DeleteLocation(location);
    }
}
