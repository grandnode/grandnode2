using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     ProviderOnly: the identity provider owns the name and the e-mail, so a provider sign-in copies them onto the
///     account. Mixed mode never touches the account.
/// </summary>
public class OidcProfileSynchronizer(
    ICustomerService customerService,
    CustomerSettings customerSettings,
    CustomerConfig customerConfig,
    OidcLoginConfig loginConfig,
    ILogger<OidcProfileSynchronizer> logger)
{
    public async Task Sync(Customer customer, ExternalRegistrationIdentity identity, string storeId)
    {
        if (!loginConfig.ProviderOnly || customer == null || identity == null)
            return;

        await SyncUserField(customer, SystemCustomerFieldNames.FirstName, identity.FirstName);
        await SyncUserField(customer, SystemCustomerFieldNames.LastName, identity.LastName);
        await SyncEmail(customer, identity, storeId);
    }

    private async Task SyncUserField(Customer customer, string key, string value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        var stored = customer.GetUserFieldFromEntity<string>(key);
        if (!string.Equals(stored, value, StringComparison.Ordinal))
            await customerService.UpdateUserField(customer, key, value);
    }

    private async Task SyncEmail(Customer customer, ExternalRegistrationIdentity identity, string storeId)
    {
        if (!identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email))
            return;

        var newEmail = identity.Email.Trim().ToLowerInvariant();
        if (string.Equals(newEmail, customer.Email, StringComparison.OrdinalIgnoreCase))
            return;

        //a break-glass account is listed by its e-mail in OidcLogin:BreakGlassAccounts: renaming it would lose the
        //emergency access
        if (loginConfig.IsBreakGlass(customer.Email))
        {
            logger.LogWarning("OpenID Connect profile sync: break-glass customer {CustomerId} keeps its e-mail " +
                              "(the identity provider has a different one)", customer.Id);
            return;
        }

        //nor may an account take an address listed there: the password it still has would open the Admin login page
        //and keep it signed in past the provider
        if (loginConfig.IsBreakGlass(newEmail))
        {
            logger.LogWarning("OpenID Connect profile sync: customer {CustomerId} keeps its e-mail: the identity " +
                              "provider's one is a break-glass address", customer.Id);
            return;
        }

        var owner = await customerService.GetCustomerByEmail(newEmail, customerConfig.RegisterCustomersPerStore ? storeId : "");
        if (owner != null && owner.Id != customer.Id)
        {
            logger.LogWarning("OpenID Connect profile sync: customer {CustomerId} keeps its e-mail: the identity " +
                              "provider's one belongs to customer {OtherId}", customer.Id, owner.Id);
            return;
        }

        await customerService.UpdateCustomerField(customer, x => x.Email, newEmail);
        customer.Email = newEmail;
        if (!customerSettings.UsernamesEnabled)
        {
            await customerService.UpdateCustomerField(customer, x => x.Username, newEmail);
            customer.Username = newEmail;
        }
    }
}
