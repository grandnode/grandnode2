using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Mediator;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Handlers.Orders;

public class GetMerchandiseReturnShippingOffersHandler(
    IReturnShippingService returnShippingService,
    ICurrencyService currencyService,
    IPriceFormatter priceFormatter,
    ITranslationService translationService)
    : IRequestHandler<GetMerchandiseReturnShippingOffers, IList<ReturnShippingOfferModel>>
{
    public async Task<IList<ReturnShippingOfferModel>> Handle(GetMerchandiseReturnShippingOffers request,
        CancellationToken cancellationToken)
    {
        var offers = await returnShippingService.GetOffers(new MerchandiseReturnSubmitRequest {
            Order = request.Order,
            //the store the order was placed in, whose return methods the return is sent back with
            StoreId = request.Order.StoreId,
            CustomerId = request.Customer.Id,
            LanguageId = request.Language.Id,
            PickupAddress = request.PickupAddress,
            Items = request.Model.Items.Select(x => new MerchandiseReturnSubmitItem(x.Id, x.Quantity,
                x.MerchandiseReturnReasonId, x.MerchandiseReturnActionId)).ToList()
        });

        //shown as the details page shows the saved cost: in the order's currency, at the order's rate
        var orderCurrency = offers.Count > 0
            ? await currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode)
            : null;
        var models = new List<ReturnShippingOfferModel>();
        foreach (var offer in offers)
            models.Add(new ReturnShippingOfferModel {
                ProviderSystemName = offer.ProviderSystemName,
                OptionCode = offer.OptionCode,
                Name = offer.Name,
                Description = offer.Description,
                Cost = offer.Cost > 0
                    ? priceFormatter.FormatPrice(offer.Cost * request.Order.CurrencyRate, orderCurrency)
                    : translationService.GetResource("MerchandiseReturns.ReturnShipping.Free", request.Language.Id),
                Parcels = offer.Parcels,
                ParcelsText = offer.Parcels > 1
                    ? string.Format(
                        translationService.GetResource("MerchandiseReturns.ReturnShipping.Parcels",
                            request.Language.Id), offer.Parcels)
                    : "",
                FormUrl = offer.HasForm && !string.IsNullOrEmpty(offer.FormRouteName)
                    ? request.FormUrl?.Invoke(offer.FormRouteName) ?? ""
                    : ""
            });

        return models;
    }
}
