using Grand.Business.Catalog.Interfaces.Products;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

            var builder = Builders<CustomerGroupProduct>.Filter;
            var filter = builder.Eq(x => x.Id, customerGroupProduct.Id);
            var update = Builders<CustomerGroupProduct>.Update
                .Set(x => x.DisplayOrder, customerGroupProduct.DisplayOrder);
            await _customerGroupProductRepository.Collection.UpdateOneAsync(filter, update);

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
            return await _cacheBase.GetAsync(key, () =>
            {
                var filter = Builders<CustomerGroupProduct>.Filter.Eq(x => x.CustomerGroupId, customerGroupId);
                return _customerGroupProductRepository.Collection.Find(filter).SortBy(x => x.DisplayOrder).ToListAsync();
            });
        }

        /// <summary>
        /// Gets customer groups products for customer group
        /// </summary>
        /// <param name="customerGroupId">Customer group id</param>
        /// <param name="productId">Product id</param>
        /// <returns>Customer group product</returns>
        public virtual Task<CustomerGroupProduct> GetCustomerGroupProduct(string customerGroupId, string productId)
        {
            var filters = Builders<CustomerGroupProduct>.Filter;
            var filter = filters.Eq(x => x.CustomerGroupId, customerGroupId);
            filter &= filters.Eq(x => x.ProductId, productId);

            return _customerGroupProductRepository.Collection.Find(filter).SortBy(x => x.DisplayOrder).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets customer groups product
        /// </summary>
        /// <param name="Id">id</param>
        /// <returns>Customer group product</returns>
        public virtual Task<CustomerGroupProduct> GetCustomerGroupProductById(string id)
        {
            var query = from cr in _customerGroupProductRepository.Table
                        where cr.Id == id
                        orderby cr.DisplayOrder
                        select cr;

            return query.FirstOrDefaultAsync();
        }


        #endregion
    }
}
