using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     The customer authenticated by the API scheme
/// </summary>
public class ApiUserCustomerResolver(IApiAuthenticationService apiAuthenticationService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.ApiUser;

    public async Task<Customer> Resolve(Store store)
    {
        return await apiAuthenticationService.GetAuthenticatedCustomer();
    }
}
