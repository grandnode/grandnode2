using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     The signed-in (cookie) customer; the work context applies admin impersonation to it
/// </summary>
public class CookieCustomerResolver(IGrandAuthenticationService authenticationService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.Cookie;

    public bool SupportsImpersonation => true;

    public async Task<Customer> Resolve(Store store)
    {
        return await authenticationService.GetAuthenticatedCustomer();
    }
}
