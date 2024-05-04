using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Common.Services.Addresses;

/// <summary>
///     Address attribute service
/// </summary>
public class AddressAttributeService : IAddressAttributeService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="addressAttributeRepository">Address attribute repository</param>
    /// <param name="mediator">Mediator</param>
    public AddressAttributeService(ICacheBase cacheBase,
        IRepository<AddressAttribute> addressAttributeRepository,
        IMediator mediator)
    {
        _cacheBase = cacheBase;
        _addressAttributeRepository = addressAttributeRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<AddressAttribute> _addressAttributeRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all address attributes
    /// </summary>
    /// <returns>Address attributes</returns>
    public virtual async Task<IList<AddressAttribute>> GetAllAddressAttributes()
    {
        var key = CacheKey.ADDRESSATTRIBUTES_ALL_KEY;
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from aa in _addressAttributeRepository.Table
                orderby aa.DisplayOrder
                select aa;
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets an address attribute
    /// </summary>
    /// <param name="addressAttributeId">Address attribute identifier</param>
    /// <returns>Address attribute</returns>
    public virtual async Task<AddressAttribute> GetAddressAttributeById(string addressAttributeId)
    {
        if (string.IsNullOrEmpty(addressAttributeId))
            return null;

        var key = string.Format(CacheKey.ADDRESSATTRIBUTES_BY_ID_KEY, addressAttributeId);
        return await _cacheBase.GetAsync(key, () => _addressAttributeRepository.GetByIdAsync(addressAttributeId));
    }

    /// <summary>
    ///     Inserts an address attribute
    /// </summary>
    /// <param name="addressAttribute">Address attribute</param>
    public virtual async Task InsertAddressAttribute(AddressAttribute addressAttribute)
    {
        ArgumentNullException.ThrowIfNull(addressAttribute);

        await _addressAttributeRepository.InsertAsync(addressAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(addressAttribute);
    }

    /// <summary>
    ///     Updates the address attribute
    /// </summary>
    /// <param name="addressAttribute">Address attribute</param>
    public virtual async Task UpdateAddressAttribute(AddressAttribute addressAttribute)
    {
        ArgumentNullException.ThrowIfNull(addressAttribute);

        await _addressAttributeRepository.UpdateAsync(addressAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(addressAttribute);
    }

    /// <summary>
    ///     Deletes an address attribute
    /// </summary>
    /// <param name="addressAttribute">Address attribute</param>
    public virtual async Task DeleteAddressAttribute(AddressAttribute addressAttribute)
    {
        ArgumentNullException.ThrowIfNull(addressAttribute);

        await _addressAttributeRepository.DeleteAsync(addressAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(addressAttribute);
    }

    /// <summary>
    ///     Inserts an address attribute value
    /// </summary>
    /// <param name="addressAttributeValue">Address attribute value</param>
    public virtual async Task InsertAddressAttributeValue(AddressAttributeValue addressAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(addressAttributeValue);

        await _addressAttributeRepository.AddToSet(addressAttributeValue.AddressAttributeId,
            x => x.AddressAttributeValues, addressAttributeValue);

        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(addressAttributeValue);
    }

    /// <summary>
    ///     Updates the address attribute value
    /// </summary>
    /// <param name="addressAttributeValue">Address attribute value</param>
    public virtual async Task UpdateAddressAttributeValue(AddressAttributeValue addressAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(addressAttributeValue);

        await _addressAttributeRepository.UpdateToSet(addressAttributeValue.AddressAttributeId,
            x => x.AddressAttributeValues, z => z.Id, addressAttributeValue.Id, addressAttributeValue);

        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(addressAttributeValue);
    }

    /// <summary>
    ///     Deletes an address attribute value
    /// </summary>
    /// <param name="addressAttributeValue">Address attribute value</param>
    public virtual async Task DeleteAddressAttributeValue(AddressAttributeValue addressAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(addressAttributeValue);

        await _addressAttributeRepository.PullFilter(addressAttributeValue.AddressAttributeId,
            x => x.AddressAttributeValues, z => z.Id, addressAttributeValue.Id);

        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(addressAttributeValue);
    }

    #endregion
}