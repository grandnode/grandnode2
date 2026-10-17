using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Authentication;

public interface ITwoFactorAuthenticationService
{
    Task<bool> AuthenticateTwoFactor(string secretKey, string token, Customer customer,
        TwoFactorAuthenticationType twoFactorAuthenticationType);

    /// <param name="store">The store whose company name the authenticator app shows as the issuer</param>
    Task<TwoFactorCodeSetup> GenerateCodeSetup(string secretKey, Customer customer, Store store, Language language,
        TwoFactorAuthenticationType twoFactorAuthenticationType);
}