using Grand.Domain.Customers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Products
{
    public interface ICustomerGroupProductService
    {
        /// <summary>
        /// Gets customer groups products for customer group
        /// </summary>
        /// <param name="customerGroupId">Customer group id</param>
        /// <returns>Customer group products</returns>
        Task<IList<CustomerGroupProduct>> GetCustomerGroupProducts(string customerGroupId);

        /// <summary>
        /// Gets customer groups products for customer group
        /// </summary>
        /// <param name="customerGroupId">Customer group id</param>
        /// <param name="productId">Product id</param>
        /// <returns>Customer group product</returns>
        Task<CustomerGroupProduct> GetCustomerGroupProduct(string customerGroupId, string productId);

        /// <summary>
        /// Gets customer groups product
        /// </summary>
        /// <param name="Id">id</param>
        /// <returns>Customer group product</returns>
        Task<CustomerGroupProduct> GetCustomerGroupProductById(string id);

        /// <summary>
        /// Inserts a customer group product
        /// </summary>
        /// <param name="customerGroupProduct">Customer group product</param>
        Task InsertCustomerGroupProduct(CustomerGroupProduct customerGroupProduct);

        /// <summary>
        /// Updates the customer group product
        /// </summary>
        /// <param name="customerGroupProduct">Customer group product</param>
        Task UpdateCustomerGroupProduct(CustomerGroupProduct customerGroupProduct);

        /// <summary>
        /// Delete a customer group product
        /// </summary>
        /// <param name="customerGroupProduct">Customer group product</param>
        Task DeleteCustomerGroupProduct(CustomerGroupProduct customerGroupProduct);
    }
}
