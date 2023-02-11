﻿using Google.Authenticator;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Grand.Business.Core.Utilities.Authentication;

namespace Grand.Business.Authentication.Services
{
    public class TwoFactorAuthenticationService : ITwoFactorAuthenticationService
    {
        private readonly IWorkContext _workContext;
        private readonly IUserFieldService _userFieldService;
        private readonly IServiceProvider _serviceProvider;
        private readonly TwoFactorAuthenticator _twoFactorAuthentication;

        public TwoFactorAuthenticationService(
            IWorkContext workContext,
            IUserFieldService userFieldService,
            IServiceProvider serviceProvider)
        {
            _workContext = workContext;
            _userFieldService = userFieldService;
            _serviceProvider = serviceProvider;
            _twoFactorAuthentication = new TwoFactorAuthenticator();
        }

        public virtual async Task<bool> AuthenticateTwoFactor(string secretKey, string token, Customer customer, TwoFactorAuthenticationType twoFactorAuthenticationType)
        {
            switch (twoFactorAuthenticationType)
            {
                case TwoFactorAuthenticationType.AppVerification:
                    return _twoFactorAuthentication.ValidateTwoFactorPIN(secretKey, token.Trim());

                case TwoFactorAuthenticationType.EmailVerification:
                    var customerToken = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorValidCode);
                    if (customerToken != token?.Trim())
                        return false;
                    var validUntil = customer.GetUserFieldFromEntity<DateTime>(SystemCustomerFieldNames.TwoFactorCodeValidUntil);
                    return validUntil >= DateTime.UtcNow;

                case TwoFactorAuthenticationType.SMSVerification:
                    var smsVerificationService = _serviceProvider.GetRequiredService<ISMSVerificationService>();
                    return await smsVerificationService.Authenticate(secretKey, token.Trim(), customer);
                default:
                    return false;
            }
        }

        public virtual async Task<TwoFactorCodeSetup> GenerateCodeSetup(string secretKey, Customer customer, Language language, TwoFactorAuthenticationType twoFactorAuthenticationType)
        {
            var model = new TwoFactorCodeSetup();

            switch (twoFactorAuthenticationType)
            {
                case TwoFactorAuthenticationType.AppVerification:
                    var setupInfo = _twoFactorAuthentication.GenerateSetupCode(_workContext.CurrentStore.CompanyName, customer.Email, secretKey, false);
                    model.CustomValues.Add("QrCodeImageUrl", setupInfo.QrCodeSetupImageUrl);
                    model.CustomValues.Add("ManualEntryQrCode", setupInfo.ManualEntryKey);
                    break;

                case TwoFactorAuthenticationType.EmailVerification:
                    var token = PrepareRandomCode();
                    await _userFieldService.SaveField(customer, SystemCustomerFieldNames.TwoFactorValidCode, token);
                    await _userFieldService.SaveField(customer, SystemCustomerFieldNames.TwoFactorCodeValidUntil, DateTime.UtcNow.AddMinutes(30));
                    model.CustomValues.Add("Token", token);
                    break;

                case TwoFactorAuthenticationType.SMSVerification:
                    var smsVerificationService = _serviceProvider.GetRequiredService<ISMSVerificationService>();
                    model = await smsVerificationService.GenerateCode(secretKey, customer, language);
                    break;

                default:
                    break;
            }
            return model;
        }

        private string PrepareRandomCode()
        {
            Random generator = new Random();
            return generator.Next(0, 999999).ToString("D6");
        }
    }
}
