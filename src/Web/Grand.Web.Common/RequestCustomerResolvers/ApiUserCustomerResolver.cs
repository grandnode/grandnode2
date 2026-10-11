using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     The customer authenticated by the API scheme; a rejected bearer token runs as the Anonymous system customer
/// </summary>
public class ApiUserCustomerResolver(
    IApiAuthenticationService apiAuthenticationService,
    IHttpContextAccessor httpContextAccessor,
    ICustomerService customerService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.ApiUser;

    public async Task<Customer> Resolve()
    {
        var customer = await apiAuthenticationService.GetAuthenticatedCustomer();
        if (customer != null) return customer;

        //an API call with an invalid or expired token must not insert a new guest (and set its cookie) on every call;
        //other schemes (Basic in front of a staging site) still fall through to a new guest
        string authorization = httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization];
        if (authorization == null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return await customerService.GetCustomerBySystemName(SystemCustomerNames.Anonymous);
    }
}
