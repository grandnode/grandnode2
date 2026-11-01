using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>
///     Locks a SCIM-managed account out at once: inactive (refused by the cookie, Frontend API and Backend API checks on the next
///     request), its provider sessions ended, the provider access tokens of its subject refused, its Frontend API refresh
///     token dropped and its password token rotated (cookies issued before do not come back with a reactivation)
/// </summary>
public class ScimAccountLock(
    ICustomerService customerService,
    ICustomerManagerService customerManagerService,
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
        //a new password token invalidates the cookies issued so far, so they stay dead after a reactivation
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.PasswordToken, Guid.NewGuid().ToString());
        logger.LogInformation("SCIM deactivated customer {CustomerId}", customer.Id);
    }

    /// <summary>
    ///     Leaves a shopper account SCIM takes over to the owner of its mailbox: whoever registered it (an e-mail nobody verified)
    ///     must not get the roles the identity provider gives. The password becomes one nobody knows (a new password token ends
    ///     the cookies), the sessions, the Frontend API refresh token, the second factor and the external sign-ins other than
    ///     OpenID Connect go. The employee sets a password through password recovery and connects at the first sign-in
    /// </summary>
    public virtual async Task ResetCredentials(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await sessionService.EndByCustomer(customer.Id);
        await links.DeleteManyAsync(x =>
            x.CustomerId == customer.Id && x.ProviderSystemName != OidcCustomerResolver.ProviderSystemName);

        //an empty value removes the user field
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.RefreshToken, "");
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.TwoFactorEnabled, "");
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.TwoFactorSecretKey, "");

        //also writes a new password token
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await customerManagerService.ChangePassword(new ChangePasswordRequest(customer.Email, PasswordFormat.Hashed, password),
            customer.StoreId);
        logger.LogInformation("SCIM reset the credentials of customer {CustomerId}", customer.Id);
    }

    public virtual async Task Activate(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await customerService.UpdateCustomerField(customer.Id, x => x.Active, true);
        customer.Active = true;
        logger.LogInformation("SCIM activated customer {CustomerId}", customer.Id);
    }
}
