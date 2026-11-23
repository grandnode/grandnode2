using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;
using Grand.Web.Common.Localization;

namespace Grand.Web.Features.Handlers.Customers;

public class
    GetPasswordRecoveryConfirmHandler : IRequestHandler<GetPasswordRecoveryConfirm, PasswordRecoveryConfirmModel>
{
    private readonly CustomerSettings _customerSettings;
    private readonly IRequestTranslationService _translationService;

    public GetPasswordRecoveryConfirmHandler(
        IRequestTranslationService translationService,
        CustomerSettings customerSettings)
    {
        _translationService = translationService;
        _customerSettings = customerSettings;
    }

    public async Task<PasswordRecoveryConfirmModel> Handle(GetPasswordRecoveryConfirm request,
        CancellationToken cancellationToken)
    {
        var model = new PasswordRecoveryConfirmModel();

        //an address without an account reads as a wrong link, so the page does not tell which addresses have one
        if (request.Customer == null || !request.Customer.IsPasswordRecoveryTokenValid(request.Token))
        {
            model.DisablePasswordChanging = true;
            model.Result = _translationService.GetResource("Account.PasswordRecovery.WrongToken");
            return model;
        }

        //validate token expiration date
        if (!request.Customer.IsPasswordRecoveryLinkExpired(_customerSettings)) return await Task.FromResult(model);

        model.DisablePasswordChanging = true;
        model.Result = _translationService.GetResource("Account.PasswordRecovery.LinkExpired");
        return await Task.FromResult(model);
    }
}