using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Customers.Services;

/// <summary>
///     Customer attribute service
/// </summary>
public class CustomerAttributeService : ICustomerAttributeService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="customerAttributeRepository">Customer attribute repository</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="accessControlConfig">Access control config</param>
    public CustomerAttributeService(ICacheBase cacheBase,
        IRepository<CustomerAttribute> customerAttributeRepository,
        IMediator mediator,
        AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _customerAttributeRepository = customerAttributeRepository;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<CustomerAttribute> _customerAttributeRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all customer attributes
    /// </summary>
    /// <returns>Customer attributes</returns>
    public virtual async Task<IList<CustomerAttribute>> GetAllCustomerAttributes()
    {
        return await GetAllCustomerAttributes(string.Empty);
    }

    /// <summary>
    ///     Gets all customer attributes for the specified store
    /// </summary>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Customer attributes</returns>
    public virtual async Task<IList<CustomerAttribute>> GetAllCustomerAttributes(string storeId)
    {
        var key = string.IsNullOrEmpty(storeId)
            ? CacheKey.CUSTOMERATTRIBUTES_ALL_KEY
            : $"{CacheKey.CUSTOMERATTRIBUTES_ALL_KEY}.{storeId}";
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from ca in _customerAttributeRepository.Table
                select ca;

            query = query.OrderBy(ca => ca.DisplayOrder);

            //Store acl
            if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
                query = from ca in query
                    where !ca.LimitedToStores || ca.Stores.Contains(storeId)
                    select ca;

            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets a customer attribute
    /// </summary>
    /// <param name="customerAttributeId">Customer attribute identifier</param>
    /// <returns>Customer attribute</returns>
    public virtual Task<CustomerAttribute> GetCustomerAttributeById(string customerAttributeId)
    {
        var key = string.Format(CacheKey.CUSTOMERATTRIBUTES_BY_ID_KEY, customerAttributeId);
        return _cacheBase.GetAsync(key, () => _customerAttributeRepository.GetByIdAsync(customerAttributeId));
    }

    /// <summary>
    ///     Inserts a customer attribute
    /// </summary>
    /// <param name="customerAttribute">Customer attribute</param>
    public virtual async Task InsertCustomerAttribute(CustomerAttribute customerAttribute)
    {
        ArgumentNullException.ThrowIfNull(customerAttribute);

        await _customerAttributeRepository.InsertAsync(customerAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(customerAttribute);
    }

    /// <summary>
    ///     Updates the customer attribute
    /// </summary>
    /// <param name="customerAttribute">Customer attribute</param>
    public virtual async Task UpdateCustomerAttribute(CustomerAttribute customerAttribute)
    {
        ArgumentNullException.ThrowIfNull(customerAttribute);

        await _customerAttributeRepository.UpdateAsync(customerAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(customerAttribute);
    }

    /// <summary>
    ///     Deletes a customer attribute
    /// </summary>
    /// <param name="customerAttribute">Customer attribute</param>
    public virtual async Task DeleteCustomerAttribute(CustomerAttribute customerAttribute)
    {
        ArgumentNullException.ThrowIfNull(customerAttribute);

        await _customerAttributeRepository.DeleteAsync(customerAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(customerAttribute);
    }


    /// <summary>
    ///     Inserts a customer attribute value
    /// </summary>
    /// <param name="customerAttributeValue">Customer attribute value</param>
    public virtual async Task InsertCustomerAttributeValue(CustomerAttributeValue customerAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(customerAttributeValue);

        var ca = await _customerAttributeRepository.GetByIdAsync(customerAttributeValue.CustomerAttributeId);
        ca.CustomerAttributeValues.Add(customerAttributeValue);

        await _customerAttributeRepository.UpdateAsync(ca);

        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(customerAttributeValue);
    }

    /// <summary>
    ///     Updates the customer attribute value
    /// </summary>
    /// <param name="customerAttributeValue">Customer attribute value</param>
    public virtual async Task UpdateCustomerAttributeValue(CustomerAttributeValue customerAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(customerAttributeValue);

        var ca = await _customerAttributeRepository.GetByIdAsync(customerAttributeValue.CustomerAttributeId);
        ca.CustomerAttributeValues.Remove(
            ca.CustomerAttributeValues.FirstOrDefault(c => c.Id == customerAttributeValue.Id));
        ca.CustomerAttributeValues.Add(customerAttributeValue);

        await _customerAttributeRepository.UpdateAsync(ca);

        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(customerAttributeValue);
    }

    /// <summary>
    ///     Deletes a customer attribute value
    /// </summary>
    /// <param name="customerAttributeValue">Customer attribute value</param>
    public virtual async Task DeleteCustomerAttributeValue(CustomerAttributeValue customerAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(customerAttributeValue);

        var ca = await _customerAttributeRepository.GetByIdAsync(customerAttributeValue.CustomerAttributeId);
        ca.CustomerAttributeValues.Remove(
            ca.CustomerAttributeValues.FirstOrDefault(c => c.Id == customerAttributeValue.Id));
        await _customerAttributeRepository.UpdateAsync(ca);

        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(customerAttributeValue);
    }

    #endregion
}