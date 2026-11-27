using Google.Authenticator;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Business.Authentication.Services;

public class TwoFactorAuthenticationService : ITwoFactorAuthenticationService
{
    private readonly IEnumerable<ISMSVerificationService> _smsVerificationService;
    private readonly TwoFactorAuthenticator _twoFactorAuthentication;
    private readonly ICustomerService _customerService;
    private readonly IResourceLock _resourceLock;

    public TwoFactorAuthenticationService(
        ICustomerService customerService,
        IEnumerable<ISMSVerificationService> smsVerificationService,
        IResourceLock resourceLock)
    {
        _customerService = customerService;
        _smsVerificationService = smsVerificationService;
        _resourceLock = resourceLock;
        _twoFactorAuthentication = new TwoFactorAuthenticator();
    }

    public virtual async Task<bool> AuthenticateTwoFactor(string? secretKey, string? token, Customer customer,
        TwoFactorAuthenticationType twoFactorAuthenticationType)
    {
        switch (twoFactorAuthenticationType)
        {
            case TwoFactorAuthenticationType.AppVerification:
                //an account without a secret key has no authenticator app set up; no code proves nothing
                return secretKey != null && token != null &&
                       _twoFactorAuthentication.ValidateTwoFactorPIN(secretKey, token.Trim());

            case TwoFactorAuthenticationType.EmailVerification:
                return await UseEmailCode(customer, token?.Trim());

            case TwoFactorAuthenticationType.SMSVerification:
                var smsVerificationService = _smsVerificationService.FirstOrDefault() ??
                                             throw new Exception("ISMSVerificationService not registered in DI container");
                return await smsVerificationService.Authenticate(secretKey, token?.Trim(), customer);
            default:
                return false;
        }
    }

    public virtual async Task<TwoFactorCodeSetup> GenerateCodeSetup(string secretKey, Customer customer,
        Store store, Language language, TwoFactorAuthenticationType twoFactorAuthenticationType)
    {
        ArgumentNullException.ThrowIfNull(store);

        var model = new TwoFactorCodeSetup();

        switch (twoFactorAuthenticationType)
        {
            case TwoFactorAuthenticationType.AppVerification:
                var setupInfo = _twoFactorAuthentication.GenerateSetupCode(store.CompanyName,
                    customer.Email, secretKey, false);
                model.CustomValues.Add("QrCodeImageUrl", setupInfo.QrCodeSetupImageUrl);
                model.CustomValues.Add("ManualEntryQrCode", setupInfo.ManualEntryKey);
                break;

            case TwoFactorAuthenticationType.EmailVerification:
                var token = PrepareRandomCode();
                await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.TwoFactorValidCode, token);
                await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.TwoFactorCodeValidUntil,
                    DateTime.UtcNow.AddMinutes(30));
                model.CustomValues.Add("Token", token);
                break;

            case TwoFactorAuthenticationType.SMSVerification:
                if (!_smsVerificationService.Any())
                    throw new Exception("ISMSVerificationService not registered in DI container");
                var smsVerificationService = _smsVerificationService.FirstOrDefault();
                model = await smsVerificationService!.GenerateCode(secretKey, customer, language);
                break;
        }

        return model;
    }

    //a code signs in once: it is read again and cleared under a lock, so two requests with it cannot both pass
    private async Task<bool> UseEmailCode(Customer customer, string? token)
    {
        await using var _ = await _resourceLock.Acquire([$"TwoFactorCode:{customer.Id}"]);

        var current = await _customerService.GetCustomerById(customer.Id);
        if (current == null)
            return false;

        var customerToken = current.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorValidCode);
        if (string.IsNullOrEmpty(customerToken) || !CodesMatch(customerToken, token))
            return false;
        var validUntil = current.GetUserFieldFromEntity<DateTime>(SystemCustomerFieldNames.TwoFactorCodeValidUntil);
        if (validUntil < DateTime.UtcNow)
            return false;

        await _customerService.UpdateUserField(current, SystemCustomerFieldNames.TwoFactorValidCode, string.Empty);
        return true;
    }

    private static string PrepareRandomCode()
    {
        return RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
    }

    private static bool CodesMatch(string expected, string? given)
    {
        return given != null &&
               CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(given));
    }
}