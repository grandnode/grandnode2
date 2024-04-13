using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Orders;
using MediatR;

namespace Grand.Web.Commands.Handler.Orders;

public class MerchandiseReturnSubmitCommandHandler : IRequestHandler<MerchandiseReturnSubmitCommand, MerchandiseReturn>
{
    private readonly IGroupService _groupService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IProductService _productService;
    private readonly IWorkContext _workContext;


    public MerchandiseReturnSubmitCommandHandler(IWorkContext workContext,
        IProductService productService,
        IMerchandiseReturnService merchandiseReturnService,
        IMessageProviderService messageProviderService,
        IGroupService groupService,
        LanguageSettings languageSettings)
    {
        _workContext = workContext;
        _productService = productService;
        _merchandiseReturnService = merchandiseReturnService;
        _messageProviderService = messageProviderService;
        _groupService = groupService;
        _languageSettings = languageSettings;
    }

    public async Task<MerchandiseReturn> Handle(
        MerchandiseReturnSubmitCommand request, CancellationToken cancellationToken)
    {
        var rr = new MerchandiseReturn {
            StoreId = _workContext.CurrentStore.Id,
            OrderId = request.Order.Id,
            CustomerId = _workContext.CurrentCustomer.Id,
            OwnerId = await _groupService.IsOwner(_workContext.CurrentCustomer)
                ? _workContext.CurrentCustomer.Id
                : _workContext.CurrentCustomer.OwnerId,
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
            var product = await _productService.GetProductById(orderItem.ProductId);
            if (product.NotReturnable) continue;

            var quantity = request.Model.Items.FirstOrDefault(x => x.Id == orderItem.Id)?.Quantity;
            var rrrId = request.Model.Items.FirstOrDefault(x => x.Id == orderItem.Id)?.MerchandiseReturnReasonId;
            var rraId = request.Model.Items.FirstOrDefault(x => x.Id == orderItem.Id)?.MerchandiseReturnActionId;

            if (quantity is not > 0) continue;

            var rrr = await _merchandiseReturnService.GetMerchandiseReturnReasonById(rrrId);
            var rra = await _merchandiseReturnService.GetMerchandiseReturnActionById(rraId);
            rr.MerchandiseReturnItems.Add(new MerchandiseReturnItem {
                RequestedAction = rra != null
                    ? rra.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)
                    : "not available",
                ReasonForReturn = rrr != null
                    ? rrr.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)
                    : "not available",
                Quantity = quantity.Value,
                OrderItemId = orderItem.Id
            });
            rr.VendorId = orderItem.VendorId;
        }

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