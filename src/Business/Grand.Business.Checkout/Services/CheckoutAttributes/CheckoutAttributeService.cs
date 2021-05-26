using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Orders;
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

namespace Grand.Business.Checkout.Services.CheckoutAttributes
{
    /// <summary>
    /// Checkout attribute service
    /// </summary>
    public partial class CheckoutAttributeService : ICheckoutAttributeService
    {
        #region Fields

        private readonly IRepository<CheckoutAttribute> _checkoutAttributeRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public CheckoutAttributeService(
            ICacheBase cacheBase,
            IRepository<CheckoutAttribute> checkoutAttributeRepository,
            IMediator mediator,
            IWorkContext workContext)
        {
            _cacheBase = cacheBase;
            _checkoutAttributeRepository = checkoutAttributeRepository;
            _mediator = mediator;
            _workContext = workContext;
        }

        #endregion

        #region Methods


        /// <summary>
        /// Gets all checkout attributes
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="excludeShippableAttributes">A value indicating whether we should exlude shippable attributes</param>
        /// <returns>Checkout attributes</returns>
        public virtual async Task<IList<CheckoutAttribute>> GetAllCheckoutAttributes(string storeId = "", bool excludeShippableAttributes = false, bool ignorAcl = false)
        {
            string key = string.Format(CacheKey.CHECKOUTATTRIBUTES_ALL_KEY, storeId, excludeShippableAttributes, ignorAcl);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _checkoutAttributeRepository.Table
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
                if (excludeShippableAttributes)
                {
                    query = query.Where(x => !x.ShippableProductRequired);
                }
                return await Task.FromResult(query.ToList());

            });
        }

        /// <summary>
        /// Gets a checkout attribute 
        /// </summary>
        /// <param name="checkoutAttributeId">Checkout attribute identifier</param>
        /// <returns>Checkout attribute</returns>
        public virtual Task<CheckoutAttribute> GetCheckoutAttributeById(string checkoutAttributeId)
        {
            string key = string.Format(CacheKey.CHECKOUTATTRIBUTES_BY_ID_KEY, checkoutAttributeId);
            return _cacheBase.GetAsync(key, () => _checkoutAttributeRepository.GetByIdAsync(checkoutAttributeId));
        }

        /// <summary>
        /// Inserts a checkout attribute
        /// </summary>
        /// <param name="checkoutAttribute">Checkout attribute</param>
        public virtual async Task InsertCheckoutAttribute(CheckoutAttribute checkoutAttribute)
        {
            if (checkoutAttribute == null)
                throw new ArgumentNullException(nameof(checkoutAttribute));

            await _checkoutAttributeRepository.InsertAsync(checkoutAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(checkoutAttribute);
        }

        /// <summary>
        /// Updates the checkout attribute
        /// </summary>
        /// <param name="checkoutAttribute">Checkout attribute</param>
        public virtual async Task UpdateCheckoutAttribute(CheckoutAttribute checkoutAttribute)
        {
            if (checkoutAttribute == null)
                throw new ArgumentNullException(nameof(checkoutAttribute));

            await _checkoutAttributeRepository.UpdateAsync(checkoutAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(checkoutAttribute);
        }
        /// <summary>
        /// Deletes a checkout attribute
        /// </summary>
        /// <param name="checkoutAttribute">Checkout attribute</param>
        public virtual async Task DeleteCheckoutAttribute(CheckoutAttribute checkoutAttribute)
        {
            if (checkoutAttribute == null)
                throw new ArgumentNullException(nameof(checkoutAttribute));

            await _checkoutAttributeRepository.DeleteAsync(checkoutAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(checkoutAttribute);
        }

        #endregion
    }
}
