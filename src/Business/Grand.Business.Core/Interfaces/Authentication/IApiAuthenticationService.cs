using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication;

public interface IApiAuthenticationService
{
    /// <summary>
    ///     Get authenticated customer
    /// </summary>
    /// <returns>Customer</returns>
    Task<Customer> GetAuthenticatedCustomer();
}