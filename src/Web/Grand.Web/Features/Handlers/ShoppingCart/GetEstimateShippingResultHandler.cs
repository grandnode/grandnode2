using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetEstimateShippingResultHandler : IRequestHandler<GetEstimateShippingResult, EstimateShippingResultModel>
    {
        private readonly IShippingService _shippingService;
        private readonly IPickupPointService _pickupPointService;
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly ITranslationService _translationService;
        private readonly IPriceFormatter _priceFormatter;

        private readonly ShippingSettings _shippingSettings;

        public GetEstimateShippingResultHandler(
            IShippingService shippingService,
            IPickupPointService pickupPointService,
            IOrderCalculationService orderTotalCalculationService,
            ITaxService taxService,
            ICurrencyService currencyService,
            ITranslationService translationService,
            IPriceFormatter priceFormatter,
            ShippingSettings shippingSettings)
        {
            _shippingService = shippingService;
            _pickupPointService = pickupPointService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _taxService = taxService;
            _currencyService = currencyService;
            _translationService = translationService;
            _priceFormatter = priceFormatter;
            _shippingSettings = shippingSettings;
        }

        public async Task<EstimateShippingResultModel> Handle(GetEstimateShippingResult request, CancellationToken cancellationToken)
        {
            var model = new EstimateShippingResultModel();

            if (request.Cart.RequiresShipping())
            {
                var address = new Address
                {
                    CountryId = request.CountryId,
                    StateProvinceId = request.StateProvinceId,
                    ZipPostalCode = request.ZipPostalCode,
                };
                GetShippingOptionResponse getShippingOptionResponse = await _shippingService
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
                            var soModel = new EstimateShippingResultModel.ShippingOptionModel
                            {
                                Name = shippingOption.Name,
                                Description = shippingOption.Description,

                            };

                            //calculate discounted and taxed rate
                            var total = await _orderTotalCalculationService.AdjustShippingRate(shippingOption.Rate, request.Cart);
                            List<ApplyDiscount> appliedDiscounts = total.appliedDiscounts;
                            double shippingTotal = total.shippingRate;

                            double rate = (await _taxService.GetShippingPrice(shippingTotal, request.Customer)).shippingPrice;
                            soModel.Price = _priceFormatter.FormatShippingPrice(rate);
                            model.ShippingOptions.Add(soModel);
                        }

                        //pickup in store?
                        if (_shippingSettings.AllowPickUpInStore)
                        {
                            var pickupPoints = await _pickupPointService.GetAllPickupPoints();
                            if (pickupPoints.Count > 0)
                            {
                                var soModel = new EstimateShippingResultModel.ShippingOptionModel
                                {
                                    Name = _translationService.GetResource("Checkout.PickUpInStore"),
                                    Description = _translationService.GetResource("Checkout.PickUpInStore.Description"),
                                };

                                double shippingTotal = pickupPoints.Max(x => x.PickupFee);
                                double rate = (await _taxService.GetShippingPrice(shippingTotal, request.Customer)).shippingPrice;
                                soModel.Price = _priceFormatter.FormatShippingPrice(rate);
                                model.ShippingOptions.Add(soModel);
                            }
                        }
                    }
                    else
                    {
                        model.Warnings.Add(_translationService.GetResource("Checkout.ShippingIsNotAllowed"));
                    }
                }
            }
            return model;
        }
    }
}
