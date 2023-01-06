﻿using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetPaymentMethodHandler : IRequestHandler<GetPaymentMethod, CheckoutPaymentMethodModel>
    {
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPaymentService _paymentService;
        private readonly ITaxService _taxService;

        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
        private readonly PaymentSettings _paymentSettings;

        public GetPaymentMethodHandler(ILoyaltyPointsService loyaltyPointsService,
            IOrderCalculationService orderTotalCalculationService,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IPaymentService paymentService,
            ITaxService taxService,
            LoyaltyPointsSettings loyaltyPointsSettings,
            PaymentSettings paymentSettings)
        {
            _loyaltyPointsService = loyaltyPointsService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _paymentService = paymentService;
            _taxService = taxService;
            _loyaltyPointsSettings = loyaltyPointsSettings;
            _paymentSettings = paymentSettings;
        }

        public async Task<CheckoutPaymentMethodModel> Handle(GetPaymentMethod request, CancellationToken cancellationToken)
        {
            var model = new CheckoutPaymentMethodModel();

            //loyalty points
            if (_loyaltyPointsSettings.Enabled)
            {
                var loyaltyPointsBalance = await _loyaltyPointsService.GetLoyaltyPointsBalance(request.Customer.Id, request.Store.Id);
                var loyaltyPointsAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(await _orderTotalCalculationService.ConvertLoyaltyPointsToAmount(loyaltyPointsBalance), request.Currency);
                if (loyaltyPointsAmount > 0 &&
                    _orderTotalCalculationService.CheckMinimumLoyaltyPointsToUseRequirement(loyaltyPointsBalance))
                {
                    model.DisplayLoyaltyPoints = true;
                    model.LoyaltyPointsAmount = _priceFormatter.FormatPrice(loyaltyPointsAmount, false);
                    model.LoyaltyPointsBalance = loyaltyPointsBalance;
                    var shoppingCartTotalBase = (await _orderTotalCalculationService.GetShoppingCartTotal(request.Cart, useLoyaltyPoints: true)).shoppingCartTotal;
                    model.LoyaltyPointsEnoughToPayForOrder = shoppingCartTotalBase is 0;
                }
            }

            //filter by country
            var paymentMethods = (await _paymentService
                .LoadActivePaymentMethods(request.Customer, request.Store.Id, request.FilterByCountryId))
                .Where(pm => pm.PaymentMethodType is PaymentMethodType.Standard or PaymentMethodType.Redirection).ToList();
            var availablepaymentMethods = new List<IPaymentProvider>();
            foreach (var pm in paymentMethods)
            {
                if (!await pm.HidePaymentMethod(request.Cart))
                    availablepaymentMethods.Add(pm);
            }

            foreach (var pm in availablepaymentMethods)
            {
                var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel
                {
                    Name = pm.FriendlyName,
                    Description = _paymentSettings.ShowPaymentDescriptions ? await pm.Description() : string.Empty,
                    PaymentMethodSystemName = pm.SystemName,
                    LogoUrl = pm.LogoURL
                };
                //payment method additional fee
                var paymentMethodAdditionalFee = await _paymentService.GetAdditionalHandlingFee(request.Cart, pm.SystemName);
                var rate = (await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, request.Customer)).paymentPrice;
                if (rate > 0)
                    pmModel.Fee = _priceFormatter.FormatPaymentMethodAdditionalFee(rate);

                model.PaymentMethods.Add(pmModel);
            }

            //find a selected (previously) payment method
            var selectedPaymentMethodSystemName = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPaymentMethod, request.Store.Id);
            if (!string.IsNullOrEmpty(selectedPaymentMethodSystemName))
            {
                var paymentMethodToSelect = model.PaymentMethods.ToList()
                    .Find(pm => pm.PaymentMethodSystemName.Equals(selectedPaymentMethodSystemName, StringComparison.OrdinalIgnoreCase));
                if (paymentMethodToSelect != null)
                    paymentMethodToSelect.Selected = true;
            }
            //if no option has been selected, do it for the first one
            if (model.PaymentMethods.FirstOrDefault(so => so.Selected) != null) return model;
            {
                var paymentMethodToSelect = model.PaymentMethods.FirstOrDefault();
                if (paymentMethodToSelect != null)
                    paymentMethodToSelect.Selected = true;
            }

            return model;

        }
    }
}
