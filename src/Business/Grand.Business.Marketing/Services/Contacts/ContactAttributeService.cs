using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Marketing.Services.Contacts;

/// <summary>
///     Contact attribute service
/// </summary>
public class ContactAttributeService : IContactAttributeService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public ContactAttributeService(ICacheBase cacheBase,
        IRepository<ContactAttribute> contactAttributeRepository,
        IMediator mediator,
        AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _contactAttributeRepository = contactAttributeRepository;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<ContactAttribute> _contactAttributeRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    #region Contact attributes

    /// <summary>
    ///     Deletes a contact attribute
    /// </summary>
    /// <param name="contactAttribute">Contact attribute</param>
    public virtual async Task DeleteContactAttribute(ContactAttribute contactAttribute)
    {
        ArgumentNullException.ThrowIfNull(contactAttribute);

        await _contactAttributeRepository.DeleteAsync(contactAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(contactAttribute);
    }

    /// <summary>
    ///     Gets all contact attributes
    /// </summary>
    /// <param name="access">Customer whose groups limit the result and the store, or every attribute of the store</param>
    /// <returns>Contact attributes</returns>
    public virtual async Task<IList<ContactAttribute>> GetAllContactAttributes(CatalogAccess access)
    {
        ArgumentNullException.ThrowIfNull(access);

        var applyAcl = !access.ShowHidden && !_accessControlConfig.IgnoreAcl;
        var allowedCustomerGroupsIds = applyAcl ? access.Customer.GetCustomerGroupIds() : [];
        var key = string.Format(CacheKey.CONTACTATTRIBUTES_ALL_KEY, access.StoreId, access.ShowHidden,
            string.Join(",", allowedCustomerGroupsIds));
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _contactAttributeRepository.Table
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
            return await _contactAttributeRepository.ToListAsync(query);
        });
    }

    /// <summary>
    ///     Gets a contact attribute
    /// </summary>
    /// <param name="contactAttributeId">Contact attribute identifier</param>
    /// <returns>Contact attribute</returns>
    public virtual Task<ContactAttribute> GetContactAttributeById(string contactAttributeId)
    {
        var key = string.Format(CacheKey.CONTACTATTRIBUTES_BY_ID_KEY, contactAttributeId);
        return _cacheBase.GetAsync(key, () => _contactAttributeRepository.GetByIdAsync(contactAttributeId));
    }

    /// <summary>
    ///     Inserts a contact attribute
    /// </summary>
    /// <param name="contactAttribute">Contact attribute</param>
    public virtual async Task InsertContactAttribute(ContactAttribute contactAttribute)
    {
        ArgumentNullException.ThrowIfNull(contactAttribute);

        await _contactAttributeRepository.InsertAsync(contactAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(contactAttribute);
    }

    /// <summary>
    ///     Updates the contact attribute
    /// </summary>
    /// <param name="contactAttribute">Contact attribute</param>
    public virtual async Task UpdateContactAttribute(ContactAttribute contactAttribute)
    {
        ArgumentNullException.ThrowIfNull(contactAttribute);

        await _contactAttributeRepository.UpdateAsync(contactAttribute);

        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(contactAttribute);
    }

    #endregion

    #endregion
}