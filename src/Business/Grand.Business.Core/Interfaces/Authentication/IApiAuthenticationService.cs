using Grand.Domain.Customers;
using Microsoft.AspNetCore.Http;

namespace Grand.Business.Core.Interfaces.Authentication
{
    public interface IApiAuthenticationService
    {
        /// <summary>
        /// Get authenticated customer
        /// </summary>
        /// <returns>Customer</returns>
        Task<Customer> GetAuthenticatedCustomer(HttpContext httpContext);
    }
}
