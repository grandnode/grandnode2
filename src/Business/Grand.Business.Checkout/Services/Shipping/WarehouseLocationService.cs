using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.Shipping;

public class WarehouseLocationService(IRepository<WarehouseLocation> locationRepository, IMediator mediator)
    : IWarehouseLocationService
{
    public Task<WarehouseLocation> GetLocationById(string id)
    {
        return locationRepository.GetByIdAsync(id);
    }

    public Task<IPagedList<WarehouseLocation>> GetLocations(IList<string> warehouseIds = null, int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        var query = locationRepository.Table;
        if (warehouseIds != null)
            query = query.Where(x => warehouseIds.Contains(x.WarehouseId));
        return locationRepository.PagedAsync(query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code), pageIndex,
            pageSize);
    }

    public async Task InsertLocation(WarehouseLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        await locationRepository.InsertAsync(location);
        await mediator.EntityInserted(location);
    }

    public async Task UpdateLocation(WarehouseLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        await locationRepository.UpdateAsync(location);
        await mediator.EntityUpdated(location);
    }

    public async Task DeleteLocation(WarehouseLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        await locationRepository.DeleteAsync(location);
        await mediator.EntityDeleted(location);
    }
}
