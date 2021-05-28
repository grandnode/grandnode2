using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetOrderTotalsHandler : IRequestHandler<GetOrderTotals, OrderTotalsModel>
    {
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPaymentService _paymentService;
        private readonly ITaxService _taxService;
        private readonly IMediator _mediator;

        private readonly TaxSettings _taxSettings;
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;

        public GetOrderTotalsHandler(
            IOrderCalculationService orderTotalCalculationService,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IPaymentService paymentService,
            ITaxService taxService,
            IMediator mediator,
            TaxSettings taxSettings,
            LoyaltyPointsSettings loyaltyPointsSettings)
        {
            _orderTotalCalculationService = orderTotalCalculationService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _paymentService = paymentService;
            _taxService = taxService;
            _mediator = mediator;
            _taxSettings = taxSettings;
            _loyaltyPointsSettings = loyaltyPointsSettings;
        }

        public async Task<OrderTotalsModel> Handle(GetOrderTotals request, CancellationToken cancellationToken)
        {
            var model = new OrderTotalsModel();
            model.IsEditable = request.IsEditable;

            if (request.Cart.Any())
            {
                //subtotal
                await PrepareSubtotal(model, request);

                //shipping info
                await PrepareShippingInfo(model, request);

                //payment method fee
                await PreparePayment(model, request);

                //tax
                await PrepareTax(model, request);

                //total
                await PrepareTotal(model, request);
            }
            return model;
        }

        private async Task PrepareSubtotal(OrderTotalsModel model, GetOrderTotals request)
        {
            var subTotalIncludingTax = request.TaxDisplayType == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
            var shoppingCartSubTotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(request.Cart, subTotalIncludingTax);
            model.SubTotal = _priceFormatter.FormatPrice(shoppingCartSubTotal.subTotalWithoutDiscount, request.Currency, request.Language, subTotalIncludingTax);
            if (shoppingCartSubTotal.discountAmount > 0)
            {
                model.SubTotalDiscount = _priceFormatter.FormatPrice(-shoppingCartSubTotal.discountAmount, request.Currency, request.Language, subTotalIncludingTax);
            }
        }

        private async Task PrepareShippingInfo(OrderTotalsModel model, GetOrderTotals request)
        {
            model.RequiresShipping = request.Cart.RequiresShipping();
            if (model.RequiresShipping)
            {
                double? shoppingCartShippingBase = (await _orderTotalCalculationService.GetShoppingCartShippingTotal(request.Cart)).shoppingCartShippingTotal;
                if (shoppingCartShippingBase.HasValue)
                {
                    model.Shipping = _priceFormatter.FormatShippingPrice(shoppingCartShippingBase.Value);

                    //selected shipping method
                    var shippingOption = request.Customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption, request.Store.Id);
                    if (shippingOption != null)
                        model.SelectedShippingMethod = shippingOption.Name;
                }
            }

        }

        private async Task PreparePayment(OrderTotalsModel model, GetOrderTotals request)
        {
            var paymentMethodSystemName = request.Customer.GetUserFieldFromEntity<string>(
                    SystemCustomerFieldNames.SelectedPaymentMethod, request.Store.Id);
            double paymentMethodAdditionalFee = await _paymentService.GetAdditionalHandlingFee(request.Cart, paymentMethodSystemName);
            double paymentMethodAdditionalFeeWithTaxBase = (await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, request.Customer)).paymentPrice;
            if (paymentMethodAdditionalFeeWithTaxBase > 0)
            {
                model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeWithTaxBase);
            }
        }

        private async Task PrepareTax(OrderTotalsModel model, GetOrderTotals request)
        {
            bool displayTax = true;
            bool displayTaxRates = true;
            if (_taxSettings.HideTaxInOrderSummary && request.TaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                var taxtotal = await _orderTotalCalculationService.GetTaxTotal(request.Cart);
                SortedDictionary<double, double> taxRates = taxtotal.taxRates;

                if (taxtotal.taxtotal == 0 && _taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                    displayTax = !displayTaxRates;

                    model.Tax = _priceFormatter.FormatPrice(taxtotal.taxtotal, false);
                    foreach (var tr in taxRates)
                    {
                        model.TaxRates.Add(new OrderTotalsModel.TaxRate
                        {
                            Rate = _priceFormatter.FormatTaxRate(tr.Key),
                            Value = _priceFormatter.FormatPrice(tr.Value, false),
                        });
                    }
                }
            }
            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;
        }

        private async Task PrepareTotal(OrderTotalsModel model, GetOrderTotals request)
        {
            var carttotal = await _orderTotalCalculationService.GetShoppingCartTotal(request.Cart);
            double? shoppingCartTotalBase = carttotal.shoppingCartTotal;
            double orderTotalDiscountAmountBase = carttotal.discountAmount;
            List<AppliedGiftVoucher> appliedGiftVouchers = carttotal.appliedGiftVouchers;
            int redeemedLoyaltyPoints = carttotal.redeemedLoyaltyPoints;
            double redeemedLoyaltyPointsAmount = carttotal.redeemedLoyaltyPointsAmount;
            if (shoppingCartTotalBase.HasValue)
            {
                model.OrderTotal = _priceFormatter.FormatPrice(shoppingCartTotalBase.Value, false);
            }
            //discount
            if (orderTotalDiscountAmountBase > 0)
            {
                model.OrderTotalDiscount = _priceFormatter.FormatPrice(-orderTotalDiscountAmountBase, false);
            }

            //gift vouchers
            if (appliedGiftVouchers != null && appliedGiftVouchers.Any())
            {
                foreach (var appliedGiftVoucher in appliedGiftVouchers)
                {
                    PrepareGiftVouchers(appliedGiftVoucher, model, request);
                }
            }

            //loyalty points to be spent (redeemed)
            if (redeemedLoyaltyPointsAmount > 0)
            {
                model.RedeemedLoyaltyPoints = redeemedLoyaltyPoints;
                model.RedeemedLoyaltyPointsAmount = _priceFormatter.FormatPrice(-redeemedLoyaltyPointsAmount, false);
            }

            //loyalty points to be earned
            if (_loyaltyPointsSettings.Enabled &&
                _loyaltyPointsSettings.DisplayHowMuchWillBeEarned &&
                shoppingCartTotalBase.HasValue)
            {
                double? shippingBaseInclTax = model.RequiresShipping
                    ? (await _orderTotalCalculationService.GetShoppingCartShippingTotal(request.Cart, true)).shoppingCartShippingTotal
                    : 0;
                var earnLoyaltyPoints = shoppingCartTotalBase.Value - shippingBaseInclTax.Value;
                if (earnLoyaltyPoints > 0)
                    model.WillEarnLoyaltyPoints = await _mediator.Send(new CalculateLoyaltyPointsCommand() { Customer = request.Customer, Amount = await _currencyService.ConvertToPrimaryStoreCurrency(earnLoyaltyPoints, request.Currency) });
            }
        }

        private void PrepareGiftVouchers(AppliedGiftVoucher appliedGiftVoucher, OrderTotalsModel model, GetOrderTotals request)
        {
            var gcModel = new OrderTotalsModel.GiftVoucher
            {
                Id = appliedGiftVoucher.GiftVoucher.Id,
                CouponCode = appliedGiftVoucher.GiftVoucher.Code,
            };
            gcModel.Amount = _priceFormatter.FormatPrice(-appliedGiftVoucher.AmountCanBeUsed, false);

            double remainingAmountBase = appliedGiftVoucher.GiftVoucher.GetGiftVoucherRemainingAmount() - appliedGiftVoucher.AmountCanBeUsed;
            gcModel.Remaining = _priceFormatter.FormatPrice(remainingAmountBase, false);

            model.GiftVouchers.Add(gcModel);
        }

    }
}
