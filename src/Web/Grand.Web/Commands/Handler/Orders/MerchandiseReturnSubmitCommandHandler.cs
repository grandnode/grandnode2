using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Orders;

public class MerchandiseReturnSubmitCommandHandler
    : IRequestHandler<MerchandiseReturnSubmitCommand, MerchandiseReturnSubmitResult>
{
    private readonly IGroupService _groupService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;
    private readonly IMerchandiseReturnMessageProviderService _merchandiseReturnMessageProviderService;
    private readonly IContextAccessor _contextAccessor;

    public MerchandiseReturnSubmitCommandHandler(IContextAccessor contextAccessor,
        IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
        IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
        IGroupService groupService,
        LanguageSettings languageSettings)
    {
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
            Items = request.Model.Items.Select(x => new MerchandiseReturnSubmitItem(x.Id, x.Quantity,
                x.MerchandiseReturnReasonId, x.MerchandiseReturnActionId)).ToList()
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
}
