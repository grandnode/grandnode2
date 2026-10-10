using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;

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
        var guid = await authenticationService.GetCustomerGuid();
        if (string.IsNullOrEmpty(guid) || !Guid.TryParse(guid, out var customerGuid)) return null;

        var customerByGuid = await customerService.GetCustomerByGuid(customerGuid);
        if (customerByGuid is { Deleted: false, Active: true } && !await groupService.IsRegistered(customerByGuid))
            return customerByGuid;

        return null;
    }
}
