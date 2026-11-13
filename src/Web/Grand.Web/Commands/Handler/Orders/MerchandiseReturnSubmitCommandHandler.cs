using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Orders;

public class MerchandiseReturnSubmitCommandHandler
    : IRequestHandler<MerchandiseReturnSubmitCommand, MerchandiseReturnSubmitResult>
{
    /// <summary>
    ///     The prefix of the fields of a return method's form
    /// </summary>
    public const string FormPrefix = "returnshipping.";

    private readonly IGroupService _groupService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;
    private readonly IMerchandiseReturnMessageProviderService _merchandiseReturnMessageProviderService;
    private readonly IContextAccessor _contextAccessor;
    private readonly OrderSettings _orderSettings;

    public MerchandiseReturnSubmitCommandHandler(IContextAccessor contextAccessor,
        IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
        IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
        IGroupService groupService,
        LanguageSettings languageSettings,
        OrderSettings orderSettings)
    {
        _orderSettings = orderSettings;
        _contextAccessor = contextAccessor;
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
        _merchandiseReturnMessageProviderService = merchandiseReturnMessageProviderService;
        _groupService = groupService;
        _languageSettings = languageSettings;
    }

    public async Task<MerchandiseReturnSubmitResult> Handle(
        MerchandiseReturnSubmitCommand request, CancellationToken cancellationToken)
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        var result = await _merchandiseReturnWorkflowService.Submit(new MerchandiseReturnSubmitRequest {
            Order = request.Order,
            //the store the order was placed in, whose staff handle its returns
            StoreId = request.Order.StoreId,
            CustomerId = customer.Id,
            OwnerId = await _groupService.IsOwner(customer) ? customer.Id : customer.OwnerId,
            LanguageId = _contextAccessor.WorkContext.WorkingLanguage.Id,
            Comments = request.Model.Comments,
            PickupAddress = request.Address,
            PickupDate = request.Model.PickupDate,
            //a store that does not ask for the requested action saves none, whatever was posted
            Items = request.Model.Items.Select(x => new MerchandiseReturnSubmitItem(x.Id, x.Quantity,
                x.MerchandiseReturnReasonId,
                _orderSettings.MerchandiseReturns_HideRequestedAction ? null : x.MerchandiseReturnActionId)).ToList(),
            ReturnShipping = ReturnShippingChoice(request)
        });

        //each vendor's return is a return of its own, with its own messages
        foreach (var merchandiseReturn in result.MerchandiseReturns)
        {
            //notify store owner here (email)
            await _merchandiseReturnMessageProviderService.SendNewMerchandiseReturnStoreOwnerMessage(merchandiseReturn,
                request.Order, _languageSettings.DefaultAdminLanguageId,
                host: _contextAccessor.StoreContext.CurrentHost);
            //notify customer
            await _merchandiseReturnMessageProviderService.SendNewMerchandiseReturnCustomerMessage(merchandiseReturn,
                request.Order, request.Order.CustomerLanguageId, host: _contextAccessor.StoreContext.CurrentHost);
        }

        return result;
    }

    /// <summary>
    ///     The return method chosen, with the fields of its form, prefix stripped; null when none was chosen
    /// </summary>
    private static ReturnShippingChoice ReturnShippingChoice(MerchandiseReturnSubmitCommand request)
    {
        if (string.IsNullOrEmpty(request.Model.ReturnShippingProvider))
            return null;

        var form = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in request.Form ?? new Dictionary<string, string>())
            if (key.StartsWith(FormPrefix, StringComparison.OrdinalIgnoreCase) && key.Length > FormPrefix.Length)
                form[key[FormPrefix.Length..]] = value;

        return new ReturnShippingChoice {
            ProviderSystemName = request.Model.ReturnShippingProvider,
            OptionCode = request.Model.ReturnShippingOption,
            Form = form
        };
    }
}
