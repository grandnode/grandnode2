using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     The signed-in (cookie) customer; the work context applies admin impersonation to it
/// </summary>
/// <remarks>
///     Sealed: the work context recognises this type to apply impersonation.
/// </remarks>
public sealed class CookieCustomerResolver(IGrandAuthenticationService authenticationService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.Cookie;

    public async Task<Customer> Resolve()
    {
        return await authenticationService.GetAuthenticatedCustomer();
    }
}
