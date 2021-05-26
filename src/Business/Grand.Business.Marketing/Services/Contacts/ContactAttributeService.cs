using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Contacts
{
    /// <summary>
    /// Contact attribute service
    /// </summary>
    public partial class ContactAttributeService : IContactAttributeService
    {
        #region Fields

        private readonly IRepository<ContactAttribute> _contactAttributeRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public ContactAttributeService(ICacheBase cacheBase,
            IRepository<ContactAttribute> contactAttributeRepository,
            IMediator mediator,
            IWorkContext workContext)
        {
            _cacheBase = cacheBase;
            _contactAttributeRepository = contactAttributeRepository;
            _mediator = mediator;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        #region Contact attributes

        /// <summary>
        /// Deletes a contact attribute
        /// </summary>
        /// <param name="contactAttribute">Contact attribute</param>
        public virtual async Task DeleteContactAttribute(ContactAttribute contactAttribute)
        {
            if (contactAttribute == null)
                throw new ArgumentNullException(nameof(contactAttribute));

            await _contactAttributeRepository.DeleteAsync(contactAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(contactAttribute);
        }

        /// <summary>
        /// Gets all contact attributes
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Contact attributes</returns>
        public virtual async Task<IList<ContactAttribute>> GetAllContactAttributes(string storeId = "", bool ignorAcl = false)
        {
            string key = string.Format(CacheKey.CONTACTATTRIBUTES_ALL_KEY, storeId, ignorAcl);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _contactAttributeRepository.Table
                            select p;

                query = query.OrderBy(c => c.DisplayOrder);

                if ((!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations) ||
                    (!ignorAcl && !CommonHelper.IgnoreAcl))
                {
                    if (!ignorAcl && !CommonHelper.IgnoreAcl)
                    {
                        var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                        query = from p in query
                                where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                                select p;
                    }
                    //Store acl
                    if (!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
                    {
                        query = from p in query
                                where !p.LimitedToStores || p.Stores.Contains(storeId)
                                select p;
                    }
                }
                return await Task.FromResult(query.ToList());

            });
        }

        /// <summary>
        /// Gets a contact attribute 
        /// </summary>
        /// <param name="contactAttributeId">Contact attribute identifier</param>
        /// <returns>Contact attribute</returns>
        public virtual Task<ContactAttribute> GetContactAttributeById(string contactAttributeId)
        {
            string key = string.Format(CacheKey.CONTACTATTRIBUTES_BY_ID_KEY, contactAttributeId);
            return _cacheBase.GetAsync(key, () => _contactAttributeRepository.GetByIdAsync(contactAttributeId));
        }

        /// <summary>
        /// Inserts a contact attribute
        /// </summary>
        /// <param name="contactAttribute">Contact attribute</param>
        public virtual async Task InsertContactAttribute(ContactAttribute contactAttribute)
        {
            if (contactAttribute == null)
                throw new ArgumentNullException(nameof(contactAttribute));

            await _contactAttributeRepository.InsertAsync(contactAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(contactAttribute);
        }

        /// <summary>
        /// Updates the contact attribute
        /// </summary>
        /// <param name="contactAttribute">Contact attribute</param>
        public virtual async Task UpdateContactAttribute(ContactAttribute contactAttribute)
        {
            if (contactAttribute == null)
                throw new ArgumentNullException(nameof(contactAttribute));

            await _contactAttributeRepository.UpdateAsync(contactAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(contactAttribute);
        }

        #endregion


        #endregion
    }
}
