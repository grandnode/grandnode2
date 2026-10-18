using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Common.Security.Authentication;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     The returning guest identified by the customer-guid cookie
/// </summary>
public class GuestCookieCustomerResolver(
    IGrandAuthenticationService authenticationService,
    ICustomerService customerService,
    IGroupService groupService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.GuestCookie;

    public async Task<Customer> Resolve()
    {
        var cookie = await authenticationService.GetCustomerGuid();
        if (!GuestCustomerRules.TryParseCookie(cookie, out var customerGuid)) return null;

        var customerByGuid = await customerService.GetCustomerByGuid(customerGuid);
        if (customerByGuid != null && await GuestCustomerRules.IsAcceptableGuest(customerByGuid, groupService))
            return customerByGuid;

        return null;
    }
}
