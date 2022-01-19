using Grand.Domain.Customers;

namespace Grand.Business.Authentication.Interfaces
{
    public partial interface IApiAuthenticationService
    {
        /// <summary>
        /// Get authenticated customer
        /// </summary>
        /// <returns>Customer</returns>
        Task<Customer> GetAuthenticatedCustomer();
    }
}
