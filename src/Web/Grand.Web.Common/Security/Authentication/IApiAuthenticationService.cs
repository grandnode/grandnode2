using Grand.Domain.Customers;

namespace Grand.Web.Common.Security.Authentication;

public interface IApiAuthenticationService
{
    /// <summary>
    ///     Get authenticated customer
    /// </summary>
    /// <returns>Customer</returns>
    Task<Customer> GetAuthenticatedCustomer();
}