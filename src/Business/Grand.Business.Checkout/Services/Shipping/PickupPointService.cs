using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Checkout.Services.Shipping;

public class PickupPointService : IPickupPointService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public PickupPointService(
        IRepository<PickupPoint> pickupPointsRepository,
        IMediator mediator,
        ICacheBase cacheBase)
    {
        _pickupPointsRepository = pickupPointsRepository;
        _mediator = mediator;
        _cacheBase = cacheBase;
    }

    #endregion

    #region Fields

    private readonly IRepository<PickupPoint> _pickupPointsRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a pickup point
    /// </summary>
    /// <param name="pickupPointId">The pickup point identifier</param>
    /// <returns>Delivery date</returns>
    public virtual Task<PickupPoint> GetPickupPointById(string pickupPointId)
    {
        var key = string.Format(CacheKey.PICKUPPOINTS_BY_ID_KEY, pickupPointId);
        return _cacheBase.GetAsync(key, () => _pickupPointsRepository.GetByIdAsync(pickupPointId));
    }

    /// <summary>
    ///     Gets all pickup points
    /// </summary>
    /// <param name="storeId">Store identifier; empty to return all pickup points</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Warehouses</returns>
    public virtual async Task<IPagedList<PickupPoint>> GetAllPickupPoints(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _pickupPointsRepository.Table;
        
        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(pp => pp.StoreId == storeId);

        query = query.OrderBy(pp => pp.DisplayOrder);
        return await PagedList<PickupPoint>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets all pickup points
    /// </summary>
    /// <returns>Warehouses</returns>
    public virtual async Task<IList<PickupPoint>> LoadActivePickupPoints(string storeId = "")
    {
        var pickupPoints = await GetAllPickupPoints();
        return pickupPoints.Where(pp => pp.StoreId == storeId || string.IsNullOrEmpty(pp.StoreId)).ToList();
    }


    /// <summary>
    ///     Inserts a pickup point
    /// </summary>
    /// <param name="pickupPoint">Pickup Point</param>
    public virtual async Task InsertPickupPoint(PickupPoint pickupPoint)
    {
        ArgumentNullException.ThrowIfNull(pickupPoint);

        await _pickupPointsRepository.InsertAsync(pickupPoint);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(pickupPoint);
    }

    /// <summary>
    ///     Updates the pickupPoint
    /// </summary>
    /// <param name="pickupPoint">Pickup Point</param>
    public virtual async Task UpdatePickupPoint(PickupPoint pickupPoint)
    {
        ArgumentNullException.ThrowIfNull(pickupPoint);

        await _pickupPointsRepository.UpdateAsync(pickupPoint);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(pickupPoint);
    }

    /// <summary>
    ///     Deletes a pickup point
    /// </summary>
    /// <param name="pickupPoint">pickup point</param>
    public virtual async Task DeletePickupPoint(PickupPoint pickupPoint)
    {
        ArgumentNullException.ThrowIfNull(pickupPoint);

        await _pickupPointsRepository.DeleteAsync(pickupPoint);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(pickupPoint);
    }

    #endregion
}