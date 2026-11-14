using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Handlers.Orders;

/// <summary>
///     The refund the return form comes to: the returns the submit would save, estimated as their refund is worked out
///     when they come back, at most what is left to refund of the order's payment
/// </summary>
public class GetMerchandiseReturnEstimateHandler(
    IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
    IMerchandiseReturnRefundService merchandiseReturnRefundService,
    IGroupService groupService,
    ICurrencyService currencyService,
    IPriceFormatter priceFormatter,
    ITranslationService translationService,
    OrderSettings orderSettings)
    : IRequestHandler<GetMerchandiseReturnEstimate, MerchandiseReturnEstimateModel>
{
    public async Task<MerchandiseReturnEstimateModel> Handle(GetMerchandiseReturnEstimate request,
        CancellationToken cancellationToken)
    {
        //asked for as the submit asks for it: what it would refuse, the estimate shows
        var preview = await merchandiseReturnWorkflowService.Preview(await request.Model.ToSubmitRequest(
            request.Order, request.Customer, groupService, request.Language.Id, request.PickupAddress, request.Form,
            orderSettings));

        var model = new MerchandiseReturnEstimateModel();
        //resource keys, or texts the provider translated already, which are shown as they are
        foreach (var error in new[] { preview.Error }.Concat(preview.ShippingErrors).Where(x => x != null))
            model.Errors.Add(translationService.GetResource(error, request.Language.Id, error));
        if (model.Errors.Count > 0)
            return model;

        var estimate = await merchandiseReturnRefundService.GetEstimate(preview.MerchandiseReturns, request.Order);
        var currency = await currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
        model.ItemsAmount = priceFormatter.FormatPrice(estimate.ItemsAmount, currency);
        model.ReturnShippingCost = priceFormatter.FormatPrice(estimate.ReturnShippingCost, currency);
        model.Amount = priceFormatter.FormatPrice(estimate.MaxAmount, currency);
        model.ToPaymentMethod = estimate.CanRefundOnline;
        return model;
    }
}
