using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Checkout.Services.Shipping;

public class DeliveryDateService : IDeliveryDateService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public DeliveryDateService(
        IRepository<DeliveryDate> deliveryDateRepository,
        IMediator mediator,
        ICacheBase cacheBase)
    {
        _deliveryDateRepository = deliveryDateRepository;
        _mediator = mediator;
        _cacheBase = cacheBase;
    }

    #endregion

    #region Fields

    private readonly IRepository<DeliveryDate> _deliveryDateRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Delivery dates

    /// <summary>
    ///     Gets a delivery date
    /// </summary>
    /// <param name="deliveryDateId">The delivery date identifier</param>
    /// <returns>Delivery date</returns>
    public virtual Task<DeliveryDate> GetDeliveryDateById(string deliveryDateId)
    {
        var key = string.Format(CacheKey.DELIVERYDATE_BY_ID_KEY, deliveryDateId);
        return _cacheBase.GetAsync(key, () => _deliveryDateRepository.GetByIdAsync(deliveryDateId));
    }

    /// <summary>
    ///     Gets all delivery dates
    /// </summary>
    /// <param name="storeId">Store identifier; empty to return all delivery dates</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Delivery dates</returns>
    public virtual async Task<IPagedList<DeliveryDate>> GetAllDeliveryDates(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _deliveryDateRepository.Table;
        
        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(dd => dd.StoreId == storeId);
        query = query.OrderBy(dd => dd.DisplayOrder);

        return await PagedList<DeliveryDate>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Inserts a delivery date
    /// </summary>
    /// <param name="deliveryDate">Delivery date</param>
    public virtual async Task InsertDeliveryDate(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);

        await _deliveryDateRepository.InsertAsync(deliveryDate);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(deliveryDate);
    }

    /// <summary>
    ///     Updates the delivery date
    /// </summary>
    /// <param name="deliveryDate">Delivery date</param>
    public virtual async Task UpdateDeliveryDate(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);

        await _deliveryDateRepository.UpdateAsync(deliveryDate);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(deliveryDate);
    }

    /// <summary>
    ///     Deletes a delivery date
    /// </summary>
    /// <param name="deliveryDate">The delivery date</param>
    public virtual async Task DeleteDeliveryDate(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);

        await _deliveryDateRepository.DeleteAsync(deliveryDate);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

        //clear product cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(deliveryDate);
    }

    #endregion
}