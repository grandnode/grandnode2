using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;

namespace Grand.Web.Features.Handlers.ShoppingCart;

public class GetEstimateShippingResultHandler : IRequestHandler<GetEstimateShippingResult, EstimateShippingResultModel>
{
    private readonly IOrderCalculationService _orderTotalCalculationService;
    private readonly IPickupPointService _pickupPointService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IShippingService _shippingService;

    private readonly ShippingSettings _shippingSettings;
    private readonly ITaxService _taxService;
    private readonly ITranslationService _translationService;

    public GetEstimateShippingResultHandler(
        IShippingService shippingService,
        IPickupPointService pickupPointService,
        IOrderCalculationService orderTotalCalculationService,
        ITaxService taxService,
        ITranslationService translationService,
        IPriceFormatter priceFormatter,
        ShippingSettings shippingSettings)
    {
        _shippingService = shippingService;
        _pickupPointService = pickupPointService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _taxService = taxService;
        _translationService = translationService;
        _priceFormatter = priceFormatter;
        _shippingSettings = shippingSettings;
    }

    public async Task<EstimateShippingResultModel> Handle(GetEstimateShippingResult request,
        CancellationToken cancellationToken)
    {
        var model = new EstimateShippingResultModel();

        if (!request.Cart.RequiresShipping()) return model;

        var address = new Address {
            CountryId = request.CountryId,
            StateProvinceId = request.StateProvinceId,
            ZipPostalCode = request.ZipPostalCode
        };
        var getShippingOptionResponse = await _shippingService
            .GetShippingOptions(request.Customer, request.Cart, address, "", request.Store);
        if (!getShippingOptionResponse.Success)
        {
            foreach (var error in getShippingOptionResponse.Errors)
                model.Warnings.Add(error);
        }
        else
        {
            if (getShippingOptionResponse.ShippingOptions.Any())
            {
                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                {
                    var soModel = new EstimateShippingResultModel.ShippingOptionModel {
                        Name = shippingOption.Name,
                        Description = shippingOption.Description
                    };

                    //calculate discounted and taxed rate
                    var total = await _orderTotalCalculationService.AdjustShippingRate(shippingOption.Rate,
                        request.Cart);
                    var shippingTotal = total.shippingRate;

                    var rate = (await _taxService.GetShippingPrice(shippingTotal, request.Customer)).shippingPrice;
                    soModel.Price = _priceFormatter.FormatPrice(rate, request.Currency);
                    model.ShippingOptions.Add(soModel);
                }

                //pickup in store?
                if (!_shippingSettings.AllowPickUpInStore) return model;
                {
                    var pickupPoints = await _pickupPointService.GetAllPickupPoints();
                    if (pickupPoints.Count <= 0) return model;
                    var soModel = new EstimateShippingResultModel.ShippingOptionModel {
                        Name = _translationService.GetResource("Checkout.PickUpInStore"),
                        Description = _translationService.GetResource("Checkout.PickUpInStore.Description")
                    };

                    var shippingTotal = pickupPoints.Max(x => x.PickupFee);
                    var rate = (await _taxService.GetShippingPrice(shippingTotal, request.Customer)).shippingPrice;
                    soModel.Price = _priceFormatter.FormatPrice(rate, request.Currency);
                    model.ShippingOptions.Add(soModel);
                }
            }
            else
            {
                model.Warnings.Add(_translationService.GetResource("Checkout.ShippingIsNotAllowed"));
            }
        }

        return model;
    }
}