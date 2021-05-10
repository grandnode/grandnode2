using Grand.Business.Authentication.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetTwoFactorAuthenticationHandler : IRequestHandler<GetTwoFactorAuthentication, CustomerInfoModel.TwoFactorAuthenticationModel>
    {
        private readonly ITwoFactorAuthenticationService _twoFactorAuthenticationService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly CustomerSettings _customerSetting;

        public GetTwoFactorAuthenticationHandler(
            ITwoFactorAuthenticationService twoFactorAuthenticationService,
            IMessageProviderService messageProviderService,
            CustomerSettings customerSetting)
        {
            _twoFactorAuthenticationService = twoFactorAuthenticationService;
            _messageProviderService = messageProviderService;
            _customerSetting = customerSetting;
        }

        public async Task<CustomerInfoModel.TwoFactorAuthenticationModel> Handle(GetTwoFactorAuthentication request, CancellationToken cancellationToken)
        {
            var secretkey = Guid.NewGuid().ToString();
            var setupInfo = await _twoFactorAuthenticationService.GenerateCodeSetup(secretkey, request.Customer, request.Language, _customerSetting.TwoFactorAuthenticationType);
            var model = new CustomerInfoModel.TwoFactorAuthenticationModel
            {
                CustomValues = setupInfo.CustomValues,
                SecretKey = secretkey,
                TwoFactorAuthenticationType = _customerSetting.TwoFactorAuthenticationType
            };
            if (_customerSetting.TwoFactorAuthenticationType == TwoFactorAuthenticationType.EmailVerification)
                await _messageProviderService.SendCustomerEmailTokenValidationMessage(request.Customer, request.Store, request.Customer.Id);

            return model;
        }
    }
}
