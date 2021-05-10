using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
            if (!(request.Customer.IsPasswordRecoveryTokenValid(request.Token)))
            {
                model.DisablePasswordChanging = true;
                model.Result = _translationService.GetResource("Account.PasswordRecovery.WrongToken");
            }

            //validate token expiration date
            if (request.Customer.IsPasswordRecoveryLinkExpired(_customerSettings))
            {
                model.DisablePasswordChanging = true;
                model.Result = _translationService.GetResource("Account.PasswordRecovery.LinkExpired");
            }
            return await Task.FromResult(model);
        }
    }
}
