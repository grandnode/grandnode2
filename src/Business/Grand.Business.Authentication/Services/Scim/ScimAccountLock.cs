using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>
///     Locks a SCIM-managed account out at once: inactive (refused by the cookie, Frontend API and Backend API checks on the next
///     request), its provider sessions ended, the provider access tokens of its subject refused and its Frontend API refresh
///     token dropped
/// </summary>
public class ScimAccountLock(
    ICustomerService customerService,
    IRepository<ExternalAuthentication> links,
    IOidcSessionService sessionService,
    IOidcTokenRevocationService tokenRevocationService,
    ILogger<ScimAccountLock> logger)
{
    public virtual async Task Deactivate(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await customerService.UpdateCustomerField(customer.Id, x => x.Active, false);
        customer.Active = false;
        await sessionService.EndByCustomer(customer.Id);

        var link = await links.GetOneAsync(x =>
            x.ProviderSystemName == OidcCustomerResolver.ProviderSystemName && x.CustomerId == customer.Id);
        if (link != null)
            await tokenRevocationService.RevokeTokens(link.ExternalIdentifier);

        //an empty value removes the user field
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.RefreshToken, "");
        logger.LogInformation("SCIM deactivated customer {CustomerId}", customer.Id);
    }

    public virtual async Task Activate(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await customerService.UpdateCustomerField(customer.Id, x => x.Active, true);
        customer.Active = true;
        logger.LogInformation("SCIM activated customer {CustomerId}", customer.Id);
    }
}
