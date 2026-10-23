using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Order calc service
/// </summary>
public class OrderCalculationService : IOrderCalculationService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="shoppingCartSubTotalService">Shopping cart subtotal service</param>
    /// <param name="shoppingCartShippingService">Shopping cart shipping service</param>
    /// <param name="shoppingCartTaxService">Shopping cart tax service</param>
    /// <param name="loyaltyPointsCalculationService">Loyalty points calculation service</param>
    /// <param name="taxService">Tax service</param>
    /// <param name="paymentService">Payment service</param>
    /// <param name="discountService">Discount service</param>
    /// <param name="discountValidationService">Discount validation service</param>
    /// <param name="giftVoucherService">Gift voucher service</param>
    /// <param name="shoppingCartSettings">Shopping cart settings</param>
    /// <param name="catalogSettings">Catalog settings</param>
    public OrderCalculationService(IShoppingCartSubTotalService shoppingCartSubTotalService,
        IShoppingCartShippingService shoppingCartShippingService,
        IShoppingCartTaxService shoppingCartTaxService,
        ILoyaltyPointsCalculationService loyaltyPointsCalculationService,
        ITaxService taxService,
        IPaymentService paymentService,
        IDiscountService discountService,
        IDiscountValidationService discountValidationService,
        IGiftVoucherService giftVoucherService,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings)
    {
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _shoppingCartShippingService = shoppingCartShippingService;
        _shoppingCartTaxService = shoppingCartTaxService;
        _loyaltyPointsCalculationService = loyaltyPointsCalculationService;
        _taxService = taxService;
        _paymentService = paymentService;
        _discountService = discountService;
        _discountValidationService = discountValidationService;
        _giftVoucherService = giftVoucherService;
        _shoppingCartSettings = shoppingCartSettings;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Fields

    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;
    private readonly IShoppingCartShippingService _shoppingCartShippingService;
    private readonly IShoppingCartTaxService _shoppingCartTaxService;
    private readonly ILoyaltyPointsCalculationService _loyaltyPointsCalculationService;
    private readonly ITaxService _taxService;
    private readonly IPaymentService _paymentService;
    private readonly IDiscountService _discountService;
    private readonly IDiscountValidationService _discountValidationService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly CatalogSettings _catalogSettings;

    #endregion

    #region Utilities

    /// <summary>
    ///     Gets an order discount (applied to order total)
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="orderTotal">Order total</param>
    /// <returns>Order discount</returns>
    protected virtual async Task<(double orderTotalDiscount, List<ApplyDiscount> appliedDiscounts)>
        GetOrderTotalDiscount(Customer customer, Store store, Currency currency, double orderTotal)
    {
        var appliedDiscounts = new List<ApplyDiscount>();
        double discountAmount = 0;
        if (_catalogSettings.IgnoreDiscounts)
            return (discountAmount, appliedDiscounts);

        var allDiscounts = await _discountService.GetActiveDiscountsByContext(DiscountType.AssignedToOrderTotal,
            store.Id, currency.CurrencyCode);
        var allowedDiscounts = new List<ApplyDiscount>();
        if (allDiscounts != null)
            foreach (var discount in allDiscounts)
            {
                var validDiscount =
                    await _discountValidationService.ValidateDiscount(discount, customer, store, currency);
                if (validDiscount.IsValid &&
                    discount.DiscountTypeId == DiscountType.AssignedToOrderTotal &&
                    allowedDiscounts.All(x => x.DiscountId != discount.Id))
                    allowedDiscounts.Add(new ApplyDiscount {
                        DiscountId = discount.Id,
                        IsCumulative = discount.IsCumulative,
                        CouponCode = validDiscount.CouponCode,
                        MaximumDiscountedQuantity = discount.MaximumDiscountedQuantity
                    });
            }

        var preferredDiscount = await _discountService.GetPreferredDiscount(allowedDiscounts, customer,
            currency, orderTotal);
        appliedDiscounts = preferredDiscount.appliedDiscount;
        discountAmount = preferredDiscount.discountAmount;

        if (discountAmount < 0)
            discountAmount = 0;

        if (_shoppingCartSettings.RoundPrices)
            discountAmount = RoundingHelper.RoundPrice(discountAmount, currency);

        return (discountAmount, appliedDiscounts);
    }

    /// <summary>
    ///     Get active gift vouchers that are applied by a customer
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="currency">Currency</param>
    /// <param name="store">Store</param>
    /// <returns>Active gift vouchers</returns>
    private async Task<IList<GiftVoucher>> GetActiveGiftVouchers(Customer customer, Currency currency, Store store)
    {
        var result = new List<GiftVoucher>();
        if (customer == null)
            return result;

        var couponCodes = customer.ParseAppliedCouponCodes(SystemCustomerFieldNames.GiftVoucherCoupons);
        foreach (var couponCode in couponCodes)
        {
            var giftVouchers = await _giftVoucherService.GetAllGiftVouchers(isGiftVoucherActivated: true,
                giftVoucherCouponCode: couponCode);
            result.AddRange(giftVouchers.Where(gc => gc.IsGiftVoucherValid(currency, store)));
        }

        return result;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Gets shopping cart total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="useLoyaltyPoints">Use Loyalty Points</param>
    /// <param name="usePaymentMethodAdditionalFee">A value indicating whether we should use payment method additional fee</param>
    /// <returns>Shopping cart total, discount amount, applied discounts, gift vouchers, loyalty point/amount</returns>
    public virtual async Task<ShoppingCartTotal> GetShoppingCartTotal(IList<ShoppingCartItem> cart,
        CalculationContext context, bool? useLoyaltyPoints = null, bool usePaymentMethodAdditionalFee = true)
    {
        var redeemedLoyaltyPoints = 0;
        double redeemedLoyaltyPointsAmount = 0;

        var paymentMethodSystemName = context.Customer.GetUserFieldFromEntity<string>(
            SystemCustomerFieldNames.SelectedPaymentMethod,
            context.Store.Id);

        //subtotal without tax
        var subTotal = await _shoppingCartSubTotalService.GetShoppingCartSubTotal(cart, context, false);
        var subTotalWithDiscountBase = subTotal.SubTotalWithDiscount;

        //shipping without tax
        var shippingTotal = await _shoppingCartShippingService.GetShoppingCartShippingTotal(cart, context, false);
        var shoppingCartShipping = shippingTotal.ShippingTotal;

        //payment method additional fee without tax
        double paymentMethodAdditionalFeeWithoutTax = 0;
        if (usePaymentMethodAdditionalFee && !string.IsNullOrEmpty(paymentMethodSystemName))
        {
            var paymentMethodAdditionalFee =
                await _paymentService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);
            paymentMethodAdditionalFeeWithoutTax =
                (await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false,
                    context.Customer, context.Store))
                .paymentPrice;
        }

        //tax
        var shoppingCartTax = (await _shoppingCartTaxService.GetTaxTotal(cart, context, usePaymentMethodAdditionalFee)).TaxTotal;

        //order total
        double resultTemp = 0;
        resultTemp += subTotalWithDiscountBase;
        if (shoppingCartShipping.HasValue) resultTemp += shoppingCartShipping.Value;

        resultTemp += paymentMethodAdditionalFeeWithoutTax;
        resultTemp += shoppingCartTax;
        if (_shoppingCartSettings.RoundPrices)
            resultTemp = RoundingHelper.RoundPrice(resultTemp, context.Currency);

        #region Order total discount

        var totalDiscount = await GetOrderTotalDiscount(context.Customer, context.Store,
            context.Currency, resultTemp);
        var discountAmount = totalDiscount.orderTotalDiscount;
        var appliedDiscounts = totalDiscount.appliedDiscounts;

        //sub totals with discount        
        if (resultTemp < discountAmount)
            discountAmount = resultTemp;

        //reduce subtotal
        resultTemp -= discountAmount;

        if (resultTemp < 0)
            resultTemp = 0;
        if (_shoppingCartSettings.RoundPrices)
            resultTemp = RoundingHelper.RoundPrice(resultTemp, context.Currency);

        #endregion

        #region Applied gift vouchers

        var appliedGiftVouchers = new List<AppliedGiftVoucher>();
        //we don't apply gift vouchers for recurring products
        var giftVouchers =
            await GetActiveGiftVouchers(context.Customer, context.Currency,
                context.Store);
        if (giftVouchers != null)
            foreach (var gc in giftVouchers)
                if (resultTemp > 0)
                {
                    var remainingAmount = gc.GetGiftVoucherRemainingAmount();
                    var amountCanBeUsed = resultTemp > remainingAmount ? remainingAmount : resultTemp;

                    //reduce subtotal
                    resultTemp -= amountCanBeUsed;

                    var appliedGiftVoucher = new AppliedGiftVoucher {
                        GiftVoucher = gc,
                        AmountCanBeUsed = amountCanBeUsed
                    };
                    appliedGiftVouchers.Add(appliedGiftVoucher);
                }

        #endregion

        if (resultTemp < 0)
            resultTemp = 0;
        if (_shoppingCartSettings.RoundPrices)
            resultTemp = RoundingHelper.RoundPrice(resultTemp, context.Currency);

        if (!shoppingCartShipping.HasValue)
            //we have errors
            return new ShoppingCartTotal(null, discountAmount, appliedDiscounts, appliedGiftVouchers,
                redeemedLoyaltyPoints, redeemedLoyaltyPointsAmount);

        var orderTotal = resultTemp;

        var loyaltyPointsRedemption = await _loyaltyPointsCalculationService.GetLoyaltyPointsRedemption(context,
            orderTotal, useLoyaltyPoints);
        redeemedLoyaltyPoints = loyaltyPointsRedemption.Points;
        redeemedLoyaltyPointsAmount = loyaltyPointsRedemption.Amount;

        orderTotal -= redeemedLoyaltyPointsAmount;
        if (_shoppingCartSettings.RoundPrices)
            orderTotal = RoundingHelper.RoundPrice(orderTotal, context.Currency);

        return new ShoppingCartTotal(orderTotal, discountAmount, appliedDiscounts, appliedGiftVouchers,
            redeemedLoyaltyPoints, redeemedLoyaltyPointsAmount);
    }

    #endregion
}
