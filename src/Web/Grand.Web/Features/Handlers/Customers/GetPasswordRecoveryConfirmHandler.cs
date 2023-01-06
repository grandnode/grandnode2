﻿using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetPasswordRecoveryConfirmHandler : IRequestHandler<GetPasswordRecoveryConfirm, PasswordRecoveryConfirmModel>
    {
        private readonly ITranslationService _translationService;
        private readonly CustomerSettings _customerSettings;

        public GetPasswordRecoveryConfirmHandler(
            ITranslationService translationService,
            CustomerSettings customerSettings)
        {
            _translationService = translationService;
            _customerSettings = customerSettings;
        }

        public async Task<PasswordRecoveryConfirmModel> Handle(GetPasswordRecoveryConfirm request, CancellationToken cancellationToken)
        {
            var model = new PasswordRecoveryConfirmModel();

            //validate token
            if (!request.Customer.IsPasswordRecoveryTokenValid(request.Token))
            {
                model.DisablePasswordChanging = true;
                model.Result = _translationService.GetResource("Account.PasswordRecovery.WrongToken");
            }

            //validate token expiration date
            if (!request.Customer.IsPasswordRecoveryLinkExpired(_customerSettings)) return await Task.FromResult(model);
            
            model.DisablePasswordChanging = true;
            model.Result = _translationService.GetResource("Account.PasswordRecovery.LinkExpired");
            return await Task.FromResult(model);
        }
    }
}
