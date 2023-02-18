using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Customers
{
    /// <summary>
    /// Customer attribute service
    /// </summary>
    public interface ICustomerAttributeService
    {
        /// <summary>
        /// Gets all customer attributes
        /// </summary>
        /// <returns>Customer attributes</returns>
        Task<IList<CustomerAttribute>> GetAllCustomerAttributes();

        /// <summary>
        /// Gets a customer attribute 
        /// </summary>
        /// <param name="customerAttributeId">Customer attribute identifier</param>
        /// <returns>Customer attribute</returns>
        Task<CustomerAttribute> GetCustomerAttributeById(string customerAttributeId);

        /// <summary>
        /// Inserts a customer attribute
        /// </summary>
        /// <param name="customerAttribute">Customer attribute</param>
        Task InsertCustomerAttribute(CustomerAttribute customerAttribute);

        /// <summary>
        /// Updates the customer attribute
        /// </summary>
        /// <param name="customerAttribute">Customer attribute</param>
        Task UpdateCustomerAttribute(CustomerAttribute customerAttribute);

        /// <summary>
        /// Deletes a customer attribute
        /// </summary>
        /// <param name="customerAttribute">Customer attribute</param>
        Task DeleteCustomerAttribute(CustomerAttribute customerAttribute);

       
        /// <summary>
        /// Inserts a customer attribute value
        /// </summary>
        /// <param name="customerAttributeValue">Customer attribute value</param>
        Task InsertCustomerAttributeValue(CustomerAttributeValue customerAttributeValue);

        /// <summary>
        /// Updates the customer attribute value
        /// </summary>
        /// <param name="customerAttributeValue">Customer attribute value</param>
        Task UpdateCustomerAttributeValue(CustomerAttributeValue customerAttributeValue);

        /// <summary>
        /// Deletes a customer attribute value
        /// </summary>
        /// <param name="customerAttributeValue">Customer attribute value</param>
        Task DeleteCustomerAttributeValue(CustomerAttributeValue customerAttributeValue);

    }
}
