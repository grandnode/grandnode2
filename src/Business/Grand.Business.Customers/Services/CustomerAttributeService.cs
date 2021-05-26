using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Services
{
    /// <summary>
    /// Customer attribute service
    /// </summary>
    public partial class CustomerAttributeService : ICustomerAttributeService
    {
        #region Fields

        private readonly IRepository<CustomerAttribute> _customerAttributeRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="customerAttributeRepository">Customer attribute repository</param>
        /// <param name="mediator">Mediator</param>
        public CustomerAttributeService(ICacheBase cacheBase,
            IRepository<CustomerAttribute> customerAttributeRepository,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _customerAttributeRepository = customerAttributeRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all customer attributes
        /// </summary>
        /// <returns>Customer attributes</returns>
        public virtual async Task<IList<CustomerAttribute>> GetAllCustomerAttributes()
        {
            string key = CacheKey.CUSTOMERATTRIBUTES_ALL_KEY;
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from ca in _customerAttributeRepository.Table
                            orderby ca.DisplayOrder
                            select ca;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a customer attribute 
        /// </summary>
        /// <param name="customerAttributeId">Customer attribute identifier</param>
        /// <returns>Customer attribute</returns>
        public virtual Task<CustomerAttribute> GetCustomerAttributeById(string customerAttributeId)
        {
            string key = string.Format(CacheKey.CUSTOMERATTRIBUTES_BY_ID_KEY, customerAttributeId);
            return _cacheBase.GetAsync(key, () => _customerAttributeRepository.GetByIdAsync(customerAttributeId));
        }

        /// <summary>
        /// Inserts a customer attribute
        /// </summary>
        /// <param name="customerAttribute">Customer attribute</param>
        public virtual async Task InsertCustomerAttribute(CustomerAttribute customerAttribute)
        {
            if (customerAttribute == null)
                throw new ArgumentNullException(nameof(customerAttribute));

            await _customerAttributeRepository.InsertAsync(customerAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(customerAttribute);
        }

        /// <summary>
        /// Updates the customer attribute
        /// </summary>
        /// <param name="customerAttribute">Customer attribute</param>
        public virtual async Task UpdateCustomerAttribute(CustomerAttribute customerAttribute)
        {
            if (customerAttribute == null)
                throw new ArgumentNullException(nameof(customerAttribute));

            await _customerAttributeRepository.UpdateAsync(customerAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(customerAttribute);
        }

        /// <summary>
        /// Deletes a customer attribute
        /// </summary>
        /// <param name="customerAttribute">Customer attribute</param>
        public virtual async Task DeleteCustomerAttribute(CustomerAttribute customerAttribute)
        {
            if (customerAttribute == null)
                throw new ArgumentNullException(nameof(customerAttribute));

            await _customerAttributeRepository.DeleteAsync(customerAttribute);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(customerAttribute);
        }

       
        /// <summary>
        /// Inserts a customer attribute value
        /// </summary>
        /// <param name="customerAttributeValue">Customer attribute value</param>
        public virtual async Task InsertCustomerAttributeValue(CustomerAttributeValue customerAttributeValue)
        {
            if (customerAttributeValue == null)
                throw new ArgumentNullException(nameof(customerAttributeValue));

            var ca = await _customerAttributeRepository.GetByIdAsync(customerAttributeValue.CustomerAttributeId);
            ca.CustomerAttributeValues.Add(customerAttributeValue);

            await _customerAttributeRepository.UpdateAsync(ca);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(customerAttributeValue);
        }

        /// <summary>
        /// Updates the customer attribute value
        /// </summary>
        /// <param name="customerAttributeValue">Customer attribute value</param>
        public virtual async Task UpdateCustomerAttributeValue(CustomerAttributeValue customerAttributeValue)
        {
            if (customerAttributeValue == null)
                throw new ArgumentNullException(nameof(customerAttributeValue));

            var ca = await _customerAttributeRepository.GetByIdAsync(customerAttributeValue.CustomerAttributeId);
            ca.CustomerAttributeValues.Remove(ca.CustomerAttributeValues.FirstOrDefault(c => c.Id == customerAttributeValue.Id));
            ca.CustomerAttributeValues.Add(customerAttributeValue);

            await _customerAttributeRepository.UpdateAsync(ca);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(customerAttributeValue);
        }

        /// <summary>
        /// Deletes a customer attribute value
        /// </summary>
        /// <param name="customerAttributeValue">Customer attribute value</param>
        public virtual async Task DeleteCustomerAttributeValue(CustomerAttributeValue customerAttributeValue)
        {
            if (customerAttributeValue == null)
                throw new ArgumentNullException(nameof(customerAttributeValue));

            var ca = await _customerAttributeRepository.GetByIdAsync(customerAttributeValue.CustomerAttributeId);
            ca.CustomerAttributeValues.Remove(ca.CustomerAttributeValues.FirstOrDefault(c => c.Id == customerAttributeValue.Id));
            await _customerAttributeRepository.UpdateAsync(ca);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(customerAttributeValue);
        }

        #endregion
    }
}
