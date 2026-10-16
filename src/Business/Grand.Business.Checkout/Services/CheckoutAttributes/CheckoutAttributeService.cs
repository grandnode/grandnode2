using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.CheckoutAttributes;

/// <summary>
///     Checkout attribute service
/// </summary>
public class CheckoutAttributeService : ICheckoutAttributeService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public CheckoutAttributeService(
        ICacheBase cacheBase,
        IRepository<CheckoutAttribute> checkoutAttributeRepository,
        IMediator mediator,
        AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _checkoutAttributeRepository = checkoutAttributeRepository;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<CheckoutAttribute> _checkoutAttributeRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all checkout attributes
    /// </summary>
    /// <param name="access">Customer whose groups limit the result and the store, or every attribute of the store</param>
    /// <param name="excludeShippableAttributes">A value indicating whether we should exclude shippable attributes</param>
    /// <returns>Checkout attributes</returns>
    public virtual async Task<IList<CheckoutAttribute>> GetAllCheckoutAttributes(CatalogAccess access,
        bool excludeShippableAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(access);

        var applyAcl = !access.ShowHidden && !_accessControlConfig.IgnoreAcl;
        var allowedCustomerGroupsIds = applyAcl ? access.Customer.GetCustomerGroupIds() : [];
        var key = string.Format(CacheKey.CHECKOUTATTRIBUTES_ALL_KEY, access.StoreId, excludeShippableAttributes,
            access.ShowHidden, string.Join(",", allowedCustomerGroupsIds));
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _checkoutAttributeRepository.Table
                select p;

            query = query.OrderBy(c => c.DisplayOrder);

            if (applyAcl)
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;

            //Store acl
            if (!string.IsNullOrEmpty(access.StoreId) && !_accessControlConfig.IgnoreStoreLimitations)
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(access.StoreId)
                    select p;

            if (excludeShippableAttributes) query = query.Where(x => !x.ShippableProductRequired);
            return await _checkoutAttributeRepository.ToListAsync(query);
        });
    }

    /// <summary>
    ///     Gets a checkout attribute
    /// </summary>
    /// <param name="checkoutAttributeId">Checkout attribute identifier</param>
    /// <returns>Checkout attribute</returns>
    public virtual Task<CheckoutAttribute> GetCheckoutAttributeById(string checkoutAttributeId)
    {
        var key = string.Format(CacheKey.CHECKOUTATTRIBUTES_BY_ID_KEY, checkoutAttributeId);
        return _cacheBase.GetAsync(key, () => _checkoutAttributeRepository.GetByIdAsync(checkoutAttributeId));
    }

    /// <summary>
    ///     Inserts a checkout attribute
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    public virtual async Task InsertCheckoutAttribute(CheckoutAttribute checkoutAttribute)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttribute);

        await _checkoutAttributeRepository.InsertAsync(checkoutAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(checkoutAttribute);
    }

    /// <summary>
    ///     Updates the checkout attribute
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    public virtual async Task UpdateCheckoutAttribute(CheckoutAttribute checkoutAttribute)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttribute);

        await _checkoutAttributeRepository.UpdateAsync(checkoutAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(checkoutAttribute);
    }

    /// <summary>
    ///     Deletes a checkout attribute
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    public virtual async Task DeleteCheckoutAttribute(CheckoutAttribute checkoutAttribute)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttribute);

        await _checkoutAttributeRepository.DeleteAsync(checkoutAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(checkoutAttribute);
    }

    #endregion
}