using Grand.Business.Catalog.Interfaces.Products;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Grand.Business.Catalog.Services.Products
{
    public class CustomerGroupProductService : ICustomerGroupProductService
    {
        private readonly IRepository<CustomerGroupProduct> _customerGroupProductRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;


        public CustomerGroupProductService(IRepository<CustomerGroupProduct> customerGroupProductRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _customerGroupProductRepository = customerGroupProductRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        #region Customer Group Products

        /// <summary>
        /// Delete a customer group product
        /// </summary>
        /// <param name="customerGroupProduct">Customer group product</param>
        public virtual async Task DeleteCustomerGroupProduct(CustomerGroupProduct customerGroupProduct)
        {
            if (customerGroupProduct == null)
                throw new ArgumentNullException(nameof(customerGroupProduct));

            await _customerGroupProductRepository.DeleteAsync(customerGroupProduct);

            //clear cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, customerGroupProduct.CustomerGroupId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);

            //event notification
            await _mediator.EntityDeleted(customerGroupProduct);
        }


        /// <summary>
        /// Inserts a customer group product
        /// </summary>
        /// <param name="customerGroupProduct">Customer group product</param>
        public virtual async Task InsertCustomerGroupProduct(CustomerGroupProduct customerGroupProduct)
        {
            if (customerGroupProduct == null)
                throw new ArgumentNullException(nameof(customerGroupProduct));

            await _customerGroupProductRepository.InsertAsync(customerGroupProduct);

            //clear cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, customerGroupProduct.CustomerGroupId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);

            //event notification
            await _mediator.EntityInserted(customerGroupProduct);
        }

        /// <summary>
        /// Updates the customer group product
        /// </summary>
        /// <param name="customerGroupProduct">Customer group product</param>
        public virtual async Task UpdateCustomerGroupProduct(CustomerGroupProduct customerGroupProduct)
        {
            if (customerGroupProduct == null)
                throw new ArgumentNullException(nameof(customerGroupProduct));

            await _customerGroupProductRepository.UpdateAsync(customerGroupProduct);

            //clear cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, customerGroupProduct.CustomerGroupId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);

            //event notification
            await _mediator.EntityUpdated(customerGroupProduct);
        }


        /// <summary>
        /// Gets customer groups products for customer group
        /// </summary>
        /// <param name="customerGroupId">Customer group id</param>
        /// <returns>Customer group products</returns>
        public virtual async Task<IList<CustomerGroupProduct>> GetCustomerGroupProducts(string customerGroupId)
        {
            string key = string.Format(CacheKey.CUSTOMERGROUPSPRODUCTS_ROLE_KEY, customerGroupId);
            return await _cacheBase.GetAsync(key, async () =>
            {
                return await Task.FromResult(_customerGroupProductRepository
                    .Table.Where(x => x.CustomerGroupId == customerGroupId)
                    .OrderBy(x => x.DisplayOrder)
                    .ToList());
            });
        }

        /// <summary>
        /// Gets customer groups products for customer group
        /// </summary>
        /// <param name="customerGroupId">Customer group id</param>
        /// <param name="productId">Product id</param>
        /// <returns>Customer group product</returns>
        public virtual async Task<CustomerGroupProduct> GetCustomerGroupProduct(string customerGroupId, string productId)
        {
            return await Task.FromResult(_customerGroupProductRepository.Table
                .Where(x => x.CustomerGroupId == customerGroupId && x.ProductId == productId)
                .OrderBy(x => x.DisplayOrder).FirstOrDefault());
        }

        /// <summary>
        /// Gets customer groups product
        /// </summary>
        /// <param name="Id">id</param>
        /// <returns>Customer group product</returns>
        public virtual async Task<CustomerGroupProduct> GetCustomerGroupProductById(string id)
        {
            var query = from cr in _customerGroupProductRepository.Table
                        where cr.Id == id
                        orderby cr.DisplayOrder
                        select cr;

            return await Task.FromResult(query.FirstOrDefault());
        }


        #endregion
    }
}
