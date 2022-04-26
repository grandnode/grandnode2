using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetShippingMethodHandler : IRequestHandler<GetShippingMethod, CheckoutShippingMethodModel>
    {
        private readonly IShippingService _shippingService;
        private readonly IUserFieldService _userFieldService;
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly ITaxService _taxService;
        private readonly IPriceFormatter _priceFormatter;

        public GetShippingMethodHandler(IShippingService shippingService,
            IUserFieldService userFieldService,
            IOrderCalculationService orderTotalCalculationService,
            ITaxService taxService,
            IPriceFormatter priceFormatter)
        {
            _shippingService = shippingService;
            _userFieldService = userFieldService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _taxService = taxService;
            _priceFormatter = priceFormatter;
        }

        public async Task<CheckoutShippingMethodModel> Handle(GetShippingMethod request, CancellationToken cancellationToken)
        {
            var model = new CheckoutShippingMethodModel();

            var getShippingOptionResponse = await _shippingService
                .GetShippingOptions(request.Customer, request.Cart, request.ShippingAddress,
                "", request.Store);

            if (getShippingOptionResponse.Success)
            {
                //performance optimization. cache returned shipping options.
                //we'll use them later (after a customer has selected an option).
                await _userFieldService.SaveField(request.Customer,
                                                       SystemCustomerFieldNames.OfferedShippingOptions,
                                                       getShippingOptionResponse.ShippingOptions,
                                                       request.Store.Id);

                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                {
                    var soModel = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        Name = shippingOption.Name,
                        Description = shippingOption.Description,
                        ShippingRateProviderSystemName = shippingOption.ShippingRateProviderSystemName,
                        ShippingOption = shippingOption,
                    };

                    //adjust rate
                    var shippingTotal = (await _orderTotalCalculationService.AdjustShippingRate(
                        shippingOption.Rate, request.Cart)).shippingRate;

                    double rateBase = (await _taxService.GetShippingPrice(shippingTotal, request.Customer)).shippingPrice;
                    soModel.Fee = _priceFormatter.FormatShippingPrice(rateBase);

                    model.ShippingMethods.Add(soModel);
                }

                //find a selected (previously) shipping method
                var selectedShippingOption = request.Customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption, request.Store.Id);
                if (selectedShippingOption != null)
                {
                    var shippingOptionToSelect = model.ShippingMethods.ToList()
                        .Find(so =>
                           !String.IsNullOrEmpty(so.Name) &&
                           so.Name.Equals(selectedShippingOption.Name, StringComparison.OrdinalIgnoreCase) &&
                           !String.IsNullOrEmpty(so.ShippingRateProviderSystemName) &&
                           so.ShippingRateProviderSystemName.Equals(selectedShippingOption.ShippingRateProviderSystemName, StringComparison.OrdinalIgnoreCase));
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }
                //if no option has been selected, do it for the first one
                if (model.ShippingMethods.FirstOrDefault(so => so.Selected) == null)
                {
                    var shippingOptionToSelect = model.ShippingMethods.FirstOrDefault();
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }
            }
            else
            {
                foreach (var error in getShippingOptionResponse.Errors)
                    model.Warnings.Add(error);
            }

            return model;
        }
    }
}
