using Grand.Data;
using Grand.Domain;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using Shipping.ShippingPoint.Domain;

namespace Shipping.ShippingPoint.Services;

public class ShippingPointService : IShippingPointService
{
    #region Constants

    internal const string PICKUP_POINT_PATTERN_KEY = "Grand.ShippingPoint.";

    #endregion

    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache base</param>
    /// <param name="ShippingPointRepository">Store pickup point repository</param>
    /// <param name="mediator">Mediator</param>
    public ShippingPointService(ICacheBase cacheBase,
        IRepository<ShippingPoints> ShippingPointRepository,
        IMediator mediator)
    {
        _cacheBase = cacheBase;
        _shippingPointRepository = ShippingPointRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;
    private readonly IRepository<ShippingPoints> _shippingPointRepository;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all pickup points
    /// </summary>
    /// <param name="storeId">The store identifier; pass "" to load all records</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Pickup points</returns>
    public virtual async Task<IPagedList<ShippingPoints>> GetAllStoreShippingPoint(string storeId = "",
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from gp in _shippingPointRepository.Table
            where gp.StoreId == storeId || string.IsNullOrEmpty(gp.StoreId) || storeId == ""
            select gp;

        var records = query.ToList();

        //paging
        return await Task.FromResult(new PagedList<ShippingPoints>(records, pageIndex, pageSize));
    }

    /// <summary>
    /// </summary>
    /// <param name="pointName"></param>
    /// <returns></returns>
    public virtual Task<ShippingPoints> GetStoreShippingPointByPointName(string pointName)
    {
        return Task.FromResult((from shippingPoint in _shippingPointRepository.Table
            where shippingPoint.ShippingPointName == pointName
            select shippingPoint).FirstOrDefault());
    }

    /// <summary>
    ///     Gets a pickup point
    /// </summary>
    /// <param name="pickupPointId">Pickup point identifier</param>
    /// <returns>Pickup point</returns>
    public virtual Task<ShippingPoints> GetStoreShippingPointById(string pickupPointId)
    {
        return _shippingPointRepository.GetByIdAsync(pickupPointId);
    }

    /// <summary>
    ///     Inserts a pickup point
    /// </summary>
    /// <param name="pickupPoint">Pickup point</param>
    public virtual async Task InsertStoreShippingPoint(ShippingPoints pickupPoint)
    {
        ArgumentNullException.ThrowIfNull(pickupPoint);

        await _shippingPointRepository.InsertAsync(pickupPoint);
        await _mediator.EntityInserted(pickupPoint);
    }

    /// <summary>
    ///     Updates the pickup point
    /// </summary>
    /// <param name="pickupPoint">Pickup point</param>
    public virtual async Task UpdateStoreShippingPoint(ShippingPoints pickupPoint)
    {
        ArgumentNullException.ThrowIfNull(pickupPoint);

        await _shippingPointRepository.UpdateAsync(pickupPoint);
        await _mediator.EntityUpdated(pickupPoint);
    }

    /// <summary>
    ///     Deletes a pickup point
    /// </summary>
    /// <param name="pickupPoint">Pickup point</param>
    public virtual async Task DeleteStoreShippingPoint(ShippingPoints pickupPoint)
    {
        ArgumentNullException.ThrowIfNull(pickupPoint);

        await _shippingPointRepository.DeleteAsync(pickupPoint);
        await _mediator.EntityDeleted(pickupPoint);
    }

    #endregion
}