using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Checkout.Services.Shipping;

public class WarehouseService : IWarehouseService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public WarehouseService(
        IRepository<Warehouse> warehouseRepository,
        IMediator mediator,
        ICacheBase cacheBase)
    {
        _warehouseRepository = warehouseRepository;
        _mediator = mediator;
        _cacheBase = cacheBase;
    }

    #endregion

    #region Fields

    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Warehouses

    /// <summary>
    ///     Gets a warehouse
    /// </summary>
    /// <param name="warehouseId">The warehouse identifier</param>
    /// <returns>Warehouse</returns>
    public virtual Task<Warehouse> GetWarehouseById(string warehouseId)
    {
        var key = string.Format(CacheKey.WAREHOUSES_BY_ID_KEY, warehouseId);
        return _cacheBase.GetAsync(key, () => _warehouseRepository.GetByIdAsync(warehouseId));
    }

    /// <summary>
    ///     Gets all warehouses
    /// </summary>
    /// <param name="storeId">Store identifier; empty to return all warehouses</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Warehouses</returns>
    public virtual async Task<IPagedList<Warehouse>> GetAllWarehouses(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _warehouseRepository.Table;
        
        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(wh => wh.StoreId == storeId);
        query = query.OrderBy(wh => wh.DisplayOrder);

        return await PagedList<Warehouse>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Inserts a warehouse
    /// </summary>
    /// <param name="warehouse">Warehouse</param>
    public virtual async Task InsertWarehouse(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);

        await _warehouseRepository.InsertAsync(warehouse);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(warehouse);
    }

    /// <summary>
    ///     Updates the warehouse
    /// </summary>
    /// <param name="warehouse">Warehouse</param>
    public virtual async Task UpdateWarehouse(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);

        await _warehouseRepository.UpdateAsync(warehouse);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(warehouse);
    }

    /// <summary>
    ///     Deletes a warehouse
    /// </summary>
    /// <param name="warehouse">The warehouse</param>
    public virtual async Task DeleteWarehouse(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);

        await _warehouseRepository.DeleteAsync(warehouse);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);
        //clear product cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(warehouse);
    }

    #endregion
}