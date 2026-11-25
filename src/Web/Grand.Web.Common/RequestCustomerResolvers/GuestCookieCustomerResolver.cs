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

    public async Task<Customer?> Resolve()
    {
        var cookie = await authenticationService.GetCustomerGuid();
        //transition, removed in 2.6: a cookie written before it was protected still finds its existing guest
        //(never creates one) and is rewritten protected, so guests keep their carts across the upgrade
        var legacyCookie = cookie == null ? await authenticationService.GetLegacyCustomerGuid() : null;
        if (!GuestCustomerRules.TryParseCookie(cookie ?? legacyCookie, out var customerGuid)) return null;

        var customerByGuid = await customerService.GetCustomerByGuid(customerGuid);
        if (customerByGuid == null || !await GuestCustomerRules.IsAcceptableGuest(customerByGuid, groupService))
            return null;

        if (legacyCookie != null)
            await authenticationService.SetCustomerGuid(customerByGuid.CustomerGuid);

        return customerByGuid;
    }
}
