using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.Extensions.Logging;

namespace Authentication.OpenIdConnect.Infrastructure;

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

        var owner = await customerService.GetCustomerByEmail(newEmail, customerConfig.RegisterCustomersPerStore ? storeId : "");
        if (owner != null && owner.Id != customer.Id)
        {
            logger.LogWarning("OpenID Connect profile sync: customer {CustomerId} keeps {OldEmail}: {NewEmail} belongs to customer {OtherId}",
                customer.Id, customer.Email, newEmail, owner.Id);
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
