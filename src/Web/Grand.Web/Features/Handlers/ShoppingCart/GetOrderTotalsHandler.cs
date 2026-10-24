using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.ShoppingCart;

public class GetOrderTotalsHandler : IRequestHandler<GetOrderTotals, OrderTotalsModel>
{
    private readonly ICurrencyService _currencyService;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly IMediator _mediator;
    private readonly IOrderCalculationService _orderTotalCalculationService;
    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;
    private readonly IShoppingCartShippingService _shoppingCartShippingService;
    private readonly IShoppingCartTaxService _shoppingCartTaxService;
    private readonly IPaymentService _paymentService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ITaxService _taxService;

    private readonly TaxSettings _taxSettings;

    public GetOrderTotalsHandler(
        IOrderCalculationService orderTotalCalculationService,
        IShoppingCartSubTotalService shoppingCartSubTotalService,
        IShoppingCartShippingService shoppingCartShippingService,
        IShoppingCartTaxService shoppingCartTaxService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        IPaymentService paymentService,
        ITaxService taxService,
        IMediator mediator,
        TaxSettings taxSettings,
        LoyaltyPointsSettings loyaltyPointsSettings)
    {
        _orderTotalCalculationService = orderTotalCalculationService;
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _shoppingCartShippingService = shoppingCartShippingService;
        _shoppingCartTaxService = shoppingCartTaxService;
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
        var model = new OrderTotalsModel {
            IsEditable = request.IsEditable
        };

        if (!request.Cart.Any()) return model;

        var context = new CalculationContext(request.Customer, request.Store, request.Currency,
            request.TaxDisplayType);

        //subtotal
        await PrepareSubtotal(model, request, context);

        //shipping info
        await PrepareShippingInfo(model, request, context);

        //payment method fee
        await PreparePayment(model, request);

        //tax
        await PrepareTax(model, request, context);

        //total
        await PrepareTotal(model, request, context);
        return model;
    }

    private async Task PrepareSubtotal(OrderTotalsModel model, GetOrderTotals request, CalculationContext context)
    {
        var subTotalIncludingTax = request.TaxDisplayType == TaxDisplayType.IncludingTax &&
                                   !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
        var shoppingCartSubTotal =
            await _shoppingCartSubTotalService.GetShoppingCartSubTotal(request.Cart, context, subTotalIncludingTax);
        model.SubTotalIncludingTax = subTotalIncludingTax;
        model.SubTotal = _priceFormatter.FormatPrice(shoppingCartSubTotal.SubTotalWithoutDiscount, request.Currency);
        model.SubTotalValue = shoppingCartSubTotal.SubTotalWithoutDiscount;
        if (shoppingCartSubTotal.DiscountAmount > 0)
        {
            model.SubTotalDiscount =
                _priceFormatter.FormatPrice(-shoppingCartSubTotal.DiscountAmount, request.Currency);
            model.SubTotalDiscountValue = -shoppingCartSubTotal.DiscountAmount;
            shoppingCartSubTotal.AppliedDiscounts.ForEach(x => model.Discounts.Add(x.DiscountId));
        }
    }

    private async Task PrepareShippingInfo(OrderTotalsModel model, GetOrderTotals request,
        CalculationContext context)
    {
        model.RequiresShipping = request.Cart.RequiresShipping();
        if (model.RequiresShipping)
        {
            var shoppingCartShippingBase =
                (await _shoppingCartShippingService.GetShoppingCartShippingTotal(request.Cart, context))
                .ShippingTotal;
            if (shoppingCartShippingBase.HasValue)
            {
                model.Shipping = _priceFormatter.FormatPrice(shoppingCartShippingBase.Value, request.Currency);

                //selected shipping method
                var shippingOption =
                    request.Customer.GetUserFieldFromEntity<ShippingOption>(
                        SystemCustomerFieldNames.SelectedShippingOption, request.Store.Id);
                if (shippingOption != null)
                    model.SelectedShippingMethod = shippingOption.Name;
            }
        }
    }

    private async Task PreparePayment(OrderTotalsModel model, GetOrderTotals request)
    {
        var paymentMethodSystemName = request.Customer.GetUserFieldFromEntity<string>(
            SystemCustomerFieldNames.SelectedPaymentMethod, request.Store.Id);
        var paymentMethodAdditionalFee =
            await _paymentService.GetAdditionalHandlingFee(request.Cart, paymentMethodSystemName);
        var paymentMethodAdditionalFeeWithTaxBase =
            (await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee,
                request.TaxDisplayType == TaxDisplayType.IncludingTax, request.Customer, request.Store))
            .paymentPrice;
        if (paymentMethodAdditionalFeeWithTaxBase > 0)
        {
            model.PaymentMethodAdditionalFee =
                _priceFormatter.FormatPrice(paymentMethodAdditionalFeeWithTaxBase, request.Currency);
            model.PaymentMethodAdditionalFeeValue = paymentMethodAdditionalFeeWithTaxBase;
        }
    }

    private async Task PrepareTax(OrderTotalsModel model, GetOrderTotals request, CalculationContext context)
    {
        bool displayTax;
        bool displayTaxRates;
        if (_taxSettings.HideTaxInOrderSummary && request.TaxDisplayType == TaxDisplayType.IncludingTax)
        {
            displayTax = false;
            displayTaxRates = false;
        }
        else
        {
            var taxtotal = await _shoppingCartTaxService.GetTaxTotal(request.Cart, context);
            var taxRates = taxtotal.TaxRates;

            if (taxtotal.TaxTotal == 0 && _taxSettings.HideZeroTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                displayTax = !displayTaxRates;

                model.Tax = _priceFormatter.FormatPrice(taxtotal.TaxTotal, request.Currency);
                foreach (var tr in taxRates)
                    model.TaxRates.Add(new OrderTotalsModel.TaxRate {
                        Rate = _priceFormatter.FormatTaxRate(tr.Key),
                        Value = _priceFormatter.FormatPrice(tr.Value, request.Currency)
                    });
            }
        }

        model.DisplayTaxRates = displayTaxRates;
        model.DisplayTax = displayTax;
    }

    private async Task PrepareTotal(OrderTotalsModel model, GetOrderTotals request, CalculationContext context)
    {
        var carttotal = await _orderTotalCalculationService.GetShoppingCartTotal(request.Cart, context);
        var shoppingCartTotalBase = carttotal.Total;
        var orderTotalDiscountAmountBase = carttotal.DiscountAmount;
        var appliedGiftVouchers = carttotal.AppliedGiftVouchers;
        var redeemedLoyaltyPoints = carttotal.RedeemedLoyaltyPoints;
        var redeemedLoyaltyPointsAmount = carttotal.RedeemedLoyaltyPointsAmount;
        if (shoppingCartTotalBase.HasValue)
        {
            model.OrderTotal = _priceFormatter.FormatPrice(shoppingCartTotalBase.Value, request.Currency);
            model.OrderTotalValue = shoppingCartTotalBase.Value;
        }

        //discount
        if (orderTotalDiscountAmountBase > 0)
        {
            model.OrderTotalDiscount = _priceFormatter.FormatPrice(-orderTotalDiscountAmountBase, request.Currency);
            model.OrderTotalDiscountValue = -orderTotalDiscountAmountBase;
            carttotal.AppliedDiscounts.ForEach(x => model.Discounts.Add(x.DiscountId));
        }

        //gift vouchers
        if (appliedGiftVouchers != null && appliedGiftVouchers.Any())
            foreach (var appliedGiftVoucher in appliedGiftVouchers)
                PrepareGiftVouchers(appliedGiftVoucher, model, request);

        //loyalty points to be spent (redeemed)
        if (redeemedLoyaltyPointsAmount > 0)
        {
            model.RedeemedLoyaltyPoints = redeemedLoyaltyPoints;
            model.RedeemedLoyaltyPointsAmount =
                _priceFormatter.FormatPrice(-redeemedLoyaltyPointsAmount, request.Currency);
        }

        //loyalty points to be earned
        if (_loyaltyPointsSettings.Enabled &&
            _loyaltyPointsSettings.DisplayHowMuchWillBeEarned &&
            shoppingCartTotalBase.HasValue)
        {
            var shippingBaseInclTax = model.RequiresShipping
                ? (await _shoppingCartShippingService.GetShoppingCartShippingTotal(request.Cart, context, true))
                .ShippingTotal
                : 0;
            var earnLoyaltyPoints = shoppingCartTotalBase.Value - (shippingBaseInclTax ?? 0);
            if (earnLoyaltyPoints > 0)
                model.WillEarnLoyaltyPoints = await _mediator.Send(new CalculateLoyaltyPointsCommand {
                    Customer = request.Customer,
                    Amount = await _currencyService.ConvertToPrimaryStoreCurrency(earnLoyaltyPoints, request.Currency)
                });
        }
    }

    private void PrepareGiftVouchers(AppliedGiftVoucher appliedGiftVoucher, OrderTotalsModel model,
        GetOrderTotals request)
    {
        var gcModel = new OrderTotalsModel.GiftVoucher {
            Id = appliedGiftVoucher.GiftVoucher.Id,
            CouponCode = appliedGiftVoucher.GiftVoucher.Code,
            Amount = _priceFormatter.FormatPrice(-appliedGiftVoucher.AmountCanBeUsed, request.Currency)
        };

        var remainingAmountBase = appliedGiftVoucher.GiftVoucher.GetGiftVoucherRemainingAmount() -
                                  appliedGiftVoucher.AmountCanBeUsed;
        gcModel.Remaining = _priceFormatter.FormatPrice(remainingAmountBase, request.Currency);

        model.GiftVouchers.Add(gcModel);
    }
}