using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Handlers.Orders;

/// <summary>
///     The refund the return form comes to: the returns the submit would save, quoted as their refund is when they come
///     back, so the customer is shown the amount the store will give back for them now
/// </summary>
public class GetMerchandiseReturnEstimateHandler(
    IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
    IMerchandiseReturnRefundService merchandiseReturnRefundService,
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
        var preview = await merchandiseReturnWorkflowService.Preview(new MerchandiseReturnSubmitRequest {
            Order = request.Order,
            StoreId = request.Order.StoreId,
            CustomerId = request.Customer.Id,
            LanguageId = request.Language.Id,
            PickupAddress = request.PickupAddress,
            Items = MerchandiseReturnSubmitCommandHandler.SubmitItems(request.Model, orderSettings),
            ReturnShipping = MerchandiseReturnSubmitCommandHandler.ReturnShippingChoice(request.Model, request.Form)
        });

        var model = new MerchandiseReturnEstimateModel();
        //resource keys, or texts the provider translated already, which are shown as they are
        foreach (var error in new[] { preview.Error }.Concat(preview.ShippingErrors).Where(x => x != null))
            model.Errors.Add(translationService.GetResource(error, request.Language.Id, error));
        if (model.Errors.Count > 0)
            return model;

        double itemsAmount = 0, returnShippingCost = 0, amount = 0;
        foreach (var merchandiseReturn in preview.MerchandiseReturns)
        {
            //not received yet, so the quote says it cannot be refunded now; its amounts are what it comes to
            var quote = await merchandiseReturnRefundService.GetQuote(merchandiseReturn, request.Order);
            itemsAmount += quote.ItemsAmount;
            returnShippingCost += quote.ReturnShippingCost;
            amount += quote.Amount;
        }

        var currency = await currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
        model.ItemsAmount = priceFormatter.FormatPrice(itemsAmount, currency);
        model.ReturnShippingCost = priceFormatter.FormatPrice(returnShippingCost, currency);
        model.Amount = priceFormatter.FormatPrice(amount, currency);
        return model;
    }
}
