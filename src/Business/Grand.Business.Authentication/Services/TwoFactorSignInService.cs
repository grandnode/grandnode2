using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Authentication.Services;

public class TwoFactorSignInService(
    ITwoFactorAuthenticationService twoFactorAuthenticationService,
    ICustomerService customerService,
    IResourceLock resourceLock,
    IMediator mediator,
    CustomerSettings customerSettings) : ITwoFactorSignInService
{
    /// <summary>
    ///     Wrong codes a customer may try before the second step locks them out
    /// </summary>
    public const int MaxWrongCodes = 5;

    //the installer's lockout length, for a store that set none
    private const int DefaultLockoutMinutes = 30;

    public async Task<TwoFactorSignInResult> CheckCode(Customer customer, string token,
        TwoFactorAuthenticationType twoFactorAuthenticationType)
    {
        ArgumentNullException.ThrowIfNull(customer);

        //parallel requests of one sign-in would each read the same count
        await using var _ = await resourceLock.Acquire([$"TwoFactorSignIn:{customer.Id}"]);

        var current = await customerService.GetCustomerById(customer.Id);
        if (current == null || current.CannotLoginUntilDateUtc > DateTime.UtcNow)
            return TwoFactorSignInResult.LockedOut;

        var wrongCodes = current.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.TwoFactorWrongCodes);
        if (!string.IsNullOrEmpty(token) && await twoFactorAuthenticationService.AuthenticateTwoFactor(
                current.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorSecretKey), token, current,
                twoFactorAuthenticationType))
        {
            if (wrongCodes > 0)
                await customerService.UpdateUserField(current, SystemCustomerFieldNames.TwoFactorWrongCodes, 0);
            return TwoFactorSignInResult.Passed;
        }

        var lockedOut = ++wrongCodes >= MaxWrongCodes;
        await customerService.UpdateUserField(current, SystemCustomerFieldNames.TwoFactorWrongCodes,
            lockedOut ? 0 : wrongCodes);
        if (lockedOut)
        {
            current.CannotLoginUntilDateUtc = DateTime.UtcNow.AddMinutes(
                customerSettings.FailedPasswordLockoutMinutes > 0
                    ? customerSettings.FailedPasswordLockoutMinutes
                    : DefaultLockoutMinutes);
            await customerService.UpdateCustomerLastLoginDate(current);
        }

        //published with the customer read under the lock: its handler writes the lockout fields back
        await mediator.Publish(new CustomerLoginFailedEvent(current,
            string.IsNullOrEmpty(token) ? "two-factor code missing" : "wrong two-factor code"));

        return lockedOut ? TwoFactorSignInResult.LockedOut : TwoFactorSignInResult.WrongCode;
    }
}
