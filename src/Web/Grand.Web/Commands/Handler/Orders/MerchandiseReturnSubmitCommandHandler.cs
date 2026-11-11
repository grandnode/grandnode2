using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Orders;

public class MerchandiseReturnSubmitCommandHandler : IRequestHandler<MerchandiseReturnSubmitCommand, MerchandiseReturn>
{
    private readonly IGroupService _groupService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;


    public MerchandiseReturnSubmitCommandHandler(IContextAccessor contextAccessor,
        IMediator mediator,
        IMerchandiseReturnService merchandiseReturnService,
        IMessageProviderService messageProviderService,
        IGroupService groupService,
        LanguageSettings languageSettings)
    {
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _merchandiseReturnService = merchandiseReturnService;
        _messageProviderService = messageProviderService;
        _groupService = groupService;
        _languageSettings = languageSettings;
    }

    public async Task<MerchandiseReturn> Handle(
        MerchandiseReturnSubmitCommand request, CancellationToken cancellationToken)
    {
        //never more than can still be returned: the customer gets the quantity they asked for or nothing, never a
        //smaller return
        var returnable = await _mediator.Send(new GetReturnableQuantitiesQuery { Order = request.Order },
            cancellationToken);
        if (request.Model.Items.Any(x => x.Quantity > (returnable.TryGetValue(x.Id, out var left) ? left : 0)))
            return null;

        var rr = new MerchandiseReturn {
            StoreId = _contextAccessor.StoreContext.CurrentStore.Id,
            OrderId = request.Order.Id,
            CustomerId = _contextAccessor.WorkContext.CurrentCustomer.Id,
            OwnerId = await _groupService.IsOwner(_contextAccessor.WorkContext.CurrentCustomer)
                ? _contextAccessor.WorkContext.CurrentCustomer.Id
                : _contextAccessor.WorkContext.CurrentCustomer.OwnerId,
            SeId = request.Order.SeId,
            CustomerComments = request.Model.Comments,
            StaffNotes = string.Empty,
            MerchandiseReturnStatus = MerchandiseReturnStatus.Pending,
            PickupAddress = request.Address
        };

        if (request.Model.PickupDate.HasValue)
            rr.PickupDate = request.Model.PickupDate.Value;

        foreach (var orderItem in request.Order.OrderItems)
        {
            var quantity = request.Model.Items.FirstOrDefault(x => x.Id == orderItem.Id)?.Quantity ?? 0;
            var rrrId = request.Model.Items.FirstOrDefault(x => x.Id == orderItem.Id)?.MerchandiseReturnReasonId;
            var rraId = request.Model.Items.FirstOrDefault(x => x.Id == orderItem.Id)?.MerchandiseReturnActionId;

            if (quantity <= 0) continue;

            var rrr = await _merchandiseReturnService.GetMerchandiseReturnReasonById(rrrId);
            var rra = await _merchandiseReturnService.GetMerchandiseReturnActionById(rraId);
            rr.MerchandiseReturnItems.Add(new MerchandiseReturnItem {
                RequestedAction = rra != null
                    ? rra.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id)
                    : "not available",
                ReasonForReturn = rrr != null
                    ? rrr.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id)
                    : "not available",
                Quantity = quantity,
                OrderItemId = orderItem.Id
            });
            rr.VendorId = orderItem.VendorId;
        }

        if (rr.MerchandiseReturnItems.Count == 0)
            return null;

        await _merchandiseReturnService.InsertMerchandiseReturn(rr);

        //notify store owner here (email)
        await _messageProviderService.SendNewMerchandiseReturnStoreOwnerMessage(rr, request.Order,
            _languageSettings.DefaultAdminLanguageId);
        //notify customer
        await _messageProviderService.SendNewMerchandiseReturnCustomerMessage(rr, request.Order,
            request.Order.CustomerLanguageId);

        return rr;
    }
}