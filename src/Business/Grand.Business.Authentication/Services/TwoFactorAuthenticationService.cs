using Google.Authenticator;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Infrastructure;

namespace Grand.Business.Authentication.Services;

public class TwoFactorAuthenticationService : ITwoFactorAuthenticationService
{
    private readonly IEnumerable<ISMSVerificationService> _smsVerificationService;
    private readonly TwoFactorAuthenticator _twoFactorAuthentication;
    private readonly ICustomerService _customerService;
    private readonly IContextAccessor _contextAccessor;

    public TwoFactorAuthenticationService(
        IContextAccessor contextAccessor,
        ICustomerService customerService,
        IEnumerable<ISMSVerificationService> smsVerificationService)
    {
        _contextAccessor = contextAccessor;
        _customerService = customerService;
        _smsVerificationService = smsVerificationService;
        _twoFactorAuthentication = new TwoFactorAuthenticator();
    }

    public virtual async Task<bool> AuthenticateTwoFactor(string secretKey, string token, Customer customer,
        TwoFactorAuthenticationType twoFactorAuthenticationType)
    {
        switch (twoFactorAuthenticationType)
        {
            case TwoFactorAuthenticationType.AppVerification:
                return _twoFactorAuthentication.ValidateTwoFactorPIN(secretKey, token.Trim());

            case TwoFactorAuthenticationType.EmailVerification:
                var customerToken =
                    customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorValidCode);
                if (customerToken != token?.Trim())
                    return false;
                var validUntil =
                    customer.GetUserFieldFromEntity<DateTime>(SystemCustomerFieldNames.TwoFactorCodeValidUntil);
                return validUntil >= DateTime.UtcNow;

            case TwoFactorAuthenticationType.SMSVerification:
                if (!_smsVerificationService.Any())
                    throw new Exception("ISMSVerificationService not registered in DI container");
                var smsVerificationService = _smsVerificationService.FirstOrDefault();
                return await smsVerificationService!.Authenticate(secretKey, token.Trim(), customer);
            default:
                return false;
        }
    }

    public virtual async Task<TwoFactorCodeSetup> GenerateCodeSetup(string secretKey, Customer customer,
        Language language, TwoFactorAuthenticationType twoFactorAuthenticationType)
    {
        var model = new TwoFactorCodeSetup();

        switch (twoFactorAuthenticationType)
        {
            case TwoFactorAuthenticationType.AppVerification:
                var setupInfo = _twoFactorAuthentication.GenerateSetupCode(_contextAccessor.StoreContext.CurrentStore.CompanyName,
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

    private string PrepareRandomCode()
    {
        var generator = new Random();
        return generator.Next(0, 999999).ToString("D6");
    }
}