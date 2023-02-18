using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IGrandAuthenticationService
    {
        /// <summary>
        /// Sign in
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="createPersistentCookie">A value indicating whether to create a persistent cookie</param>
        Task SignIn(Customer customer, bool createPersistentCookie);

        /// <summary>
        /// Sign out
        /// </summary>
        Task SignOut();

        /// <summary>
        /// Get authenticated customer
        /// </summary>
        /// <returns>Customer</returns>
        Task<Customer> GetAuthenticatedCustomer();

        /// <summary>
        /// Get customer guid
        /// </summary>
        /// <returns>Customer</returns>
        Task<string> GetCustomerGuid();

        /// <summary>
        /// Set customer guid
        /// </summary>
        /// <param name="customerGuid">Guid of the customer</param>
        Task SetCustomerGuid(Guid customerGuid);


    }
}