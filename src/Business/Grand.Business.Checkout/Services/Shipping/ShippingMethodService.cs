using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Checkout.Services.Shipping;

public class ShippingMethodService : IShippingMethodService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public ShippingMethodService(
        IRepository<ShippingMethod> shippingMethodRepository,
        IMediator mediator,
        ICacheBase cacheBase)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _mediator = mediator;
        _cacheBase = cacheBase;
    }

    #endregion

    #region Fields

    private readonly IRepository<ShippingMethod> _shippingMethodRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Shipping methods

    /// <summary>
    ///     Deletes a shipping method
    /// </summary>
    /// <param name="shippingMethod">The shipping method</param>
    public virtual async Task DeleteShippingMethod(ShippingMethod shippingMethod)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);

        await _shippingMethodRepository.DeleteAsync(shippingMethod);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(shippingMethod);
    }

    /// <summary>
    ///     Gets a shipping method
    /// </summary>
    /// <param name="shippingMethodId">The shipping method identifier</param>
    /// <returns>Shipping method</returns>
    public virtual Task<ShippingMethod> GetShippingMethodById(string shippingMethodId)
    {
        var key = string.Format(CacheKey.SHIPPINGMETHOD_BY_ID_KEY, shippingMethodId);
        return _cacheBase.GetAsync(key, () => _shippingMethodRepository.GetByIdAsync(shippingMethodId));
    }

    /// <summary>
    ///     Gets all shipping methods
    /// </summary>
    /// <param name="filterByCountryId">The country ident to filter by</param>
    /// <param name="customer"></param>
    /// <returns>Shipping methods</returns>
    public virtual async Task<IList<ShippingMethod>> GetAllShippingMethods(string filterByCountryId = "",
        Customer customer = null)
    {
        var shippingMethods = await _cacheBase.GetAsync(CacheKey.SHIPPINGMETHOD_ALL, async () =>
        {
            var query = from sm in _shippingMethodRepository.Table
                orderby sm.DisplayOrder
                select sm;
            return await Task.FromResult(query.ToList());
        });

        if (!string.IsNullOrEmpty(filterByCountryId))
            shippingMethods = shippingMethods.Where(x => !x.CountryRestrictionExists(filterByCountryId)).ToList();
        if (customer != null)
            shippingMethods = shippingMethods
                .Where(x => !x.CustomerGroupRestrictionExists(customer.Groups.Select(y => y).ToList())).ToList();

        return shippingMethods;
    }

    /// <summary>
    ///     Inserts a shipping method
    /// </summary>
    /// <param name="shippingMethod">Shipping method</param>
    public virtual async Task InsertShippingMethod(ShippingMethod shippingMethod)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);

        await _shippingMethodRepository.InsertAsync(shippingMethod);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(shippingMethod);
    }

    /// <summary>
    ///     Updates the shipping method
    /// </summary>
    /// <param name="shippingMethod">Shipping method</param>
    public virtual async Task UpdateShippingMethod(ShippingMethod shippingMethod)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);

        await _shippingMethodRepository.UpdateAsync(shippingMethod);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(shippingMethod);
    }

    #endregion
}