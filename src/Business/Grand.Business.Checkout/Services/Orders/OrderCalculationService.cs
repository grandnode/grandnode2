using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.SharedKernel;

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
    /// <param name="taxService">Tax service</param>
    /// <param name="shippingService">Shipping service</param>
    /// <param name="paymentService">Payment service</param>
    /// <param name="discountService">Discount service</param>
    /// <param name="giftVoucherService">Gift voucher service</param>
    /// <param name="loyaltyPointsService">Loyalty points service</param>
    /// <param name="currencyService">Currency service</param>
    /// <param name="groupService">Group</param>
    /// <param name="discountValidationService">Discount validation service</param>
    /// <param name="taxSettings">Tax settings</param>
    /// <param name="loyaltyPointsSettings">Loyalty points settings</param>
    /// <param name="shippingSettings">Shipping settings</param>
    /// <param name="shoppingCartSettings">Shopping cart settings</param>
    /// <param name="catalogSettings">Catalog settings</param>
    public OrderCalculationService(IShoppingCartSubTotalService shoppingCartSubTotalService,
        ITaxService taxService,
        IShippingService shippingService,
        IPaymentService paymentService,
        IDiscountService discountService,
        IGiftVoucherService giftVoucherService,
        ILoyaltyPointsService loyaltyPointsService,
        ICurrencyService currencyService,
        IGroupService groupService,
        IDiscountValidationService discountValidationService,
        TaxSettings taxSettings,
        LoyaltyPointsSettings loyaltyPointsSettings,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings)
    {
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _taxService = taxService;
        _shippingService = shippingService;
        _paymentService = paymentService;
        _discountService = discountService;
        _giftVoucherService = giftVoucherService;
        _loyaltyPointsService = loyaltyPointsService;
        _currencyService = currencyService;
        _groupService = groupService;
        _discountValidationService = discountValidationService;
        _taxSettings = taxSettings;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _shippingSettings = shippingSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Fields

    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;
    private readonly ITaxService _taxService;
    private readonly IShippingService _shippingService;
    private readonly IPaymentService _paymentService;
    private readonly IDiscountService _discountService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly ILoyaltyPointsService _loyaltyPointsService;
    private readonly ICurrencyService _currencyService;
    private readonly IGroupService _groupService;
    private readonly IDiscountValidationService _discountValidationService;
    private readonly TaxSettings _taxSettings;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly ShippingSettings _shippingSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly CatalogSettings _catalogSettings;

    #endregion

    #region Utilities

    /// <summary>
    ///     Gets a shipping discount
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="shippingTotal">Shipping total</param>
    /// <returns>Shipping discount</returns>
    protected virtual async Task<(double shippingDiscount, List<ApplyDiscount> appliedDiscounts)>
        GetShippingDiscount(Customer customer, Store store, Currency currency, double shippingTotal)
    {
        var appliedDiscounts = new List<ApplyDiscount>();
        double shippingDiscountAmount = 0;
        if (_catalogSettings.IgnoreDiscounts)
            return (shippingDiscountAmount, appliedDiscounts);

        var allDiscounts = await _discountService.GetActiveDiscountsByContext(DiscountType.AssignedToShipping,
            store.Id, currency.CurrencyCode);
        var allowedDiscounts = new List<ApplyDiscount>();
        if (allDiscounts != null)
            foreach (var discount in allDiscounts)
            {
                var validDiscount =
                    await _discountValidationService.ValidateDiscount(discount, customer, store, currency);
                if (validDiscount.IsValid &&
                    discount.DiscountTypeId == DiscountType.AssignedToShipping &&
                    allowedDiscounts.All(x => x.DiscountId != discount.Id))
                    allowedDiscounts.Add(new ApplyDiscount {
                        DiscountId = discount.Id,
                        IsCumulative = discount.IsCumulative,
                        CouponCode = validDiscount.CouponCode,
                        MaximumDiscountedQuantity = discount.MaximumDiscountedQuantity
                    });
            }

        var (appliedDiscount, discountAmount) = await _discountService.GetPreferredDiscount(allowedDiscounts,
            customer, currency, shippingTotal);
        appliedDiscounts = appliedDiscount;
        shippingDiscountAmount = discountAmount;

        if (shippingDiscountAmount < 0)
            shippingDiscountAmount = 0;

        if (_shoppingCartSettings.RoundPrices)
            shippingDiscountAmount =
                RoundingHelper.RoundPrice(shippingDiscountAmount, currency);

        return (shippingDiscountAmount, appliedDiscounts);
    }

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
    ///     Gets shopping cart subtotal
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    /// <remarks>
    ///     Not virtual: payment providers read the subtotal from <see cref="IShoppingCartSubTotalService" />,
    ///     so an override here would make the order total and the payment fee disagree.
    ///     Customize the subtotal (and its discount) in <see cref="ShoppingCartSubTotalService" /> instead.
    /// </remarks>
    public Task<(double discountAmount, List<ApplyDiscount> appliedDiscounts,
            double subTotalWithoutDiscount, double subTotalWithDiscount, SortedDictionary<double, double> taxRates)>
        GetShoppingCartSubTotal(IList<ShoppingCartItem> cart, CalculationContext context, bool includingTax)
    {
        return _shoppingCartSubTotalService.GetShoppingCartSubTotal(cart, context, includingTax);
    }


    /// <summary>
    ///     Gets shopping cart additional shipping charge
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Additional shipping charge</returns>
    public virtual async Task<double> GetShoppingCartAdditionalShippingCharge(IList<ShoppingCartItem> cart,
        CalculationContext context)
    {
        var isFreeShipping = await IsFreeShipping(cart, context);
        if (isFreeShipping)
            return 0;

        return cart.Where(sci => sci.IsShipEnabled && !sci.IsFreeShipping).Sum(sci =>
            _shippingSettings.AdditionalShippingChargeByQty
                ? sci.AdditionalShippingChargeProduct * sci.Quantity
                : sci.AdditionalShippingChargeProduct);
    }

    /// <summary>
    ///     Gets a value indicating whether shipping is free
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>A value indicating whether shipping is free</returns>
    public virtual async Task<bool> IsFreeShipping(IList<ShoppingCartItem> cart, CalculationContext context)
    {
        var customer = context.Customer;
        if (customer != null)
        {
            //check whether customer has a free shipping
            if (customer.FreeShipping)
                return true;

            //check whether customer is in a customer group with free shipping applied
            var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
            if (customerGroups.Any(customerGroup => customerGroup.FreeShipping)) return true;
        }

        var shoppingCartRequiresShipping = cart.RequiresShipping();
        if (!shoppingCartRequiresShipping)
            return true;

        //check whether all shopping cart items are marked as free shipping
        var allItemsAreFreeShipping = cart.All(sc => !sc.IsShipEnabled || sc.IsFreeShipping);
        if (allItemsAreFreeShipping)
            return true;

        //free shipping over $X
        if (!_shippingSettings.FreeShippingOverXEnabled) return false;
        //check whether we have subtotal enough to have free shipping
        var (_, _, _, subTotalWithDiscount, _) =
            await GetShoppingCartSubTotal(cart, context, _shippingSettings.FreeShippingOverXIncludingTax);

        return subTotalWithDiscount > _shippingSettings.FreeShippingOverXValue;
        //otherwise, return false
    }

    /// <summary>
    ///     Adjust shipping rate (free shipping, additional charges, discounts)
    /// </summary>
    /// <param name="shippingRate">Shipping rate to adjust</param>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Adjusted shipping rate</returns>
    public virtual async Task<(double shippingRate, List<ApplyDiscount> appliedDiscounts)> AdjustShippingRate(
        double shippingRate, IList<ShoppingCartItem> cart, CalculationContext context)
    {
        var appliedDiscounts = new List<ApplyDiscount>();

        //free shipping
        if (await IsFreeShipping(cart, context))
            return (0, appliedDiscounts);

        //additional shipping charges
        var additionalShippingCharge = await GetShoppingCartAdditionalShippingCharge(cart, context);
        var adjustedRate = shippingRate + additionalShippingCharge;

        //discount
        var (discountAmount, applyDiscounts) = await GetShippingDiscount(
            context.Customer,
            context.Store,
            context.Currency, adjustedRate);
        appliedDiscounts = applyDiscounts;

        adjustedRate -= discountAmount;

        if (adjustedRate < 0)
            adjustedRate = 0;

        if (_shoppingCartSettings.RoundPrices)
            adjustedRate = RoundingHelper.RoundPrice(adjustedRate, context.Currency);

        return (adjustedRate, appliedDiscounts);
    }

    /// <summary>
    ///     Gets shopping cart shipping total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Shipping total</returns>
    public virtual async
        Task<(double? shoppingCartShippingTotal, double taxRate, List<ApplyDiscount> appliedDiscounts)>
        GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart, CalculationContext context)
    {
        var includingTax = context.IncludingTax;
        return await GetShoppingCartShippingTotal(cart, context, includingTax);
    }


    /// <summary>
    ///     Gets shopping cart shipping total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    /// <returns>Shipping total</returns>
    public virtual async
        Task<(double? shoppingCartShippingTotal, double taxRate, List<ApplyDiscount> appliedDiscounts)>
        GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart, CalculationContext context, bool includingTax)
    {
        double? shippingTotal = null;
        var appliedDiscounts = new List<ApplyDiscount>();
        double taxRate = 0;

        var customer = context.Customer;
        var store = context.Store;
        var currency = await _currencyService.GetPrimaryExchangeRateCurrency();

        var isFreeShipping = await IsFreeShipping(cart, context);
        if (isFreeShipping)
            return (0, taxRate, appliedDiscounts);

        ShippingOption shippingOption = null;
        if (customer != null)
            shippingOption =
                customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption,
                    context.Store.Id);

        if (shippingOption != null)
        {
            var rate = shippingOption.Rate;
            var adjustShippingRate = await AdjustShippingRate(rate, cart, context);
            shippingTotal = adjustShippingRate.shippingRate;
            appliedDiscounts = adjustShippingRate.appliedDiscounts;
        }
        else
        {
            //use fixed rate (if possible)
            Address shippingAddress = null;
            if (customer != null)
                shippingAddress = customer.ShippingAddress;

            var shippingRateMethods =
                await _shippingService.LoadActiveShippingRateCalculationProviders(context.Customer,
                    context.Store.Id, cart);

            if (!shippingRateMethods.Any() && !_shippingSettings.AllowPickUpInStore)
                throw new GrandException("Shipping rate  method could not be loaded");

            if (shippingRateMethods.Count == 1)
            {
                var shippingRateMethod = shippingRateMethods[0];

                var shippingOptionRequest = await _shippingService.CreateShippingOptionRequests(customer, cart,
                    shippingAddress,
                    context.Store);

                double? fixedRate = null;
                //calculate fixed rates for each request-package
                var fixedRateTmp = await shippingRateMethod.GetFixedRate(shippingOptionRequest);
                if (fixedRateTmp.HasValue)
                {
                    fixedRate = 0;
                    fixedRate += fixedRateTmp.Value;
                }

                if (fixedRate.HasValue)
                {
                    //adjust shipping rate
                    var adjustShippingRate = await AdjustShippingRate(fixedRate.Value, cart, context);
                    shippingTotal = adjustShippingRate.shippingRate;
                    appliedDiscounts = adjustShippingRate.appliedDiscounts;
                }
            }
        }

        switch (shippingTotal)
        {
            case null:
                return (null, taxRate, appliedDiscounts);
            case < 0:
                shippingTotal = 0;
                break;
        }

        //round
        if (_shoppingCartSettings.RoundPrices)
            shippingTotal = RoundingHelper.RoundPrice(shippingTotal.Value, currency);

        var shippingPrice = await _taxService.GetShippingPrice(shippingTotal.Value, includingTax, customer, store);
        double? shippingTotalTaxed = shippingPrice.shippingPrice;
        taxRate = shippingPrice.taxRate;

        //round
        if (_shoppingCartSettings.RoundPrices)
            shippingTotalTaxed = RoundingHelper.RoundPrice(shippingTotalTaxed.Value, currency);

        return (shippingTotalTaxed, taxRate, appliedDiscounts);
    }

    /// <summary>
    ///     Gets tax
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="usePaymentMethodAdditionalFee">
    ///     A value indicating whether we should use payment method additional fee when
    ///     calculating tax
    /// </param>
    /// <returns>Tax total</returns>
    public virtual async Task<(double taxtotal, SortedDictionary<double, double> taxRates)> GetTaxTotal(
        IList<ShoppingCartItem> cart, CalculationContext context, bool usePaymentMethodAdditionalFee = true)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var taxRates = new SortedDictionary<double, double>();

        var customer = context.Customer;
        var store = context.Store;

        var paymentMethodSystemName = "";
        if (customer != null)
            paymentMethodSystemName = customer.GetUserFieldFromEntity<string>(
                SystemCustomerFieldNames.SelectedPaymentMethod,
                context.Store.Id);

        //order sub total (items + checkout attributes)
        double subTotalTaxTotal = 0;

        var shoppingCartSubTotal = await GetShoppingCartSubTotal(cart, context, false);
        var orderSubTotalTaxRates = shoppingCartSubTotal.taxRates;

        foreach (var (taxRate, taxValue) in orderSubTotalTaxRates)
        {
            subTotalTaxTotal += taxValue;

            if (!(taxRate > 0) || !(taxValue > 0)) continue;
            if (!taxRates.ContainsKey(taxRate))
                taxRates.Add(taxRate, taxValue);
            else
                taxRates[taxRate] += taxValue;
        }

        //shipping
        double shippingTax = 0;
        if (_taxSettings.ShippingIsTaxable)
        {
            var shippingTotalExcl = await GetShoppingCartShippingTotal(cart, context, false);
            var shippingExclTax = shippingTotalExcl.shoppingCartShippingTotal;

            var (shippingInclTax, taxRate, _) = await GetShoppingCartShippingTotal(cart, context, true);

            if (shippingExclTax.HasValue && shippingInclTax.HasValue)
            {
                shippingTax = shippingInclTax.Value - shippingExclTax.Value;
                //ensure that tax is equal or greater than zero
                if (shippingTax < 0)
                    shippingTax = 0;

                //tax rates
                if (taxRate > 0 && shippingTax > 0)
                {
                    if (!taxRates.ContainsKey(taxRate))
                        taxRates.Add(taxRate, shippingTax);
                    else
                        taxRates[taxRate] += shippingTax;
                }
            }
        }

        //payment method additional fee
        double paymentMethodAdditionalFeeTax = 0;
        if (usePaymentMethodAdditionalFee && _taxSettings.PaymentMethodAdditionalFeeIsTaxable)
        {
            var paymentMethodAdditionalFee =
                await _paymentService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);

            var additionalFeeExclTax =
                await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false, customer, store);
            var paymentMethodAdditionalFeeExclTax = additionalFeeExclTax.paymentPrice;

            var (paymentMethodAdditionalFeeInclTax, taxRate) =
                await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, true, customer, store);
            paymentMethodAdditionalFeeTax = paymentMethodAdditionalFeeInclTax - paymentMethodAdditionalFeeExclTax;
            //ensure that tax is equal or greater than zero
            if (paymentMethodAdditionalFeeTax < 0)
                paymentMethodAdditionalFeeTax = 0;

            //tax rates
            if (taxRate > 0 && paymentMethodAdditionalFeeTax > 0)
            {
                if (!taxRates.ContainsKey(taxRate))
                    taxRates.Add(taxRate, paymentMethodAdditionalFeeTax);
                else
                    taxRates[taxRate] += paymentMethodAdditionalFeeTax;
            }
        }

        //add at least one tax rate (0%)
        if (!taxRates.Any())
            taxRates.Add(0, 0);

        //summarize taxes
        var taxTotal = subTotalTaxTotal + shippingTax + paymentMethodAdditionalFeeTax;
        //ensure that tax is equal or greater than zero
        if (taxTotal < 0)
            taxTotal = 0;
        //round tax
        if (_shoppingCartSettings.RoundPrices)
            taxTotal = RoundingHelper.RoundPrice(taxTotal, context.Currency);

        return (taxTotal, taxRates);
    }


    /// <summary>
    ///     Gets shopping cart total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="useLoyaltyPoints">Use Loyalty Points</param>
    /// <param name="usePaymentMethodAdditionalFee">A value indicating whether we should use payment method additional fee</param>
    /// <returns>Shopping cart total, discount amount, applied discounts, gift vouchers, loyalty point/amount</returns>
    public virtual async Task<(double? shoppingCartTotal, double discountAmount, List<ApplyDiscount>
            appliedDiscounts, List<AppliedGiftVoucher> appliedGiftVouchers,
            int redeemedLoyaltyPoints, double redeemedLoyaltyPointsAmount)>
        GetShoppingCartTotal(IList<ShoppingCartItem> cart, CalculationContext context, bool? useLoyaltyPoints = null,
            bool usePaymentMethodAdditionalFee = true)
    {
        var redeemedLoyaltyPoints = 0;
        double redeemedLoyaltyPointsAmount = 0;

        var paymentMethodSystemName = context.Customer.GetUserFieldFromEntity<string>(
            SystemCustomerFieldNames.SelectedPaymentMethod,
            context.Store.Id);

        //subtotal without tax
        var subTotal = await GetShoppingCartSubTotal(cart, context, false);
        var subTotalWithDiscountBase = subTotal.subTotalWithDiscount;

        //shipping without tax
        var shippingTotal = await GetShoppingCartShippingTotal(cart, context, false);
        var shoppingCartShipping = shippingTotal.shoppingCartShippingTotal;

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
        var shoppingCartTax = (await GetTaxTotal(cart, context, usePaymentMethodAdditionalFee)).taxtotal;

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
            return (null, discountAmount, appliedDiscounts, appliedGiftVouchers, redeemedLoyaltyPoints,
                redeemedLoyaltyPointsAmount);

        var orderTotal = resultTemp;

        #region Loyalty points

        if (_loyaltyPointsSettings.Enabled)
        {
            useLoyaltyPoints ??= context.Customer.GetUserFieldFromEntity<bool>(
                SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout,
                context.Store.Id);

            if (useLoyaltyPoints.Value)
            {
                var loyaltyPointsBalance =
                    await _loyaltyPointsService.GetLoyaltyPointsBalance(context.Customer.Id,
                        context.Store.Id);
                if (CheckMinimumLoyaltyPointsToUseRequirement(loyaltyPointsBalance))
                {
                    var loyaltyPointsBalanceAmount = await ConvertLoyaltyPointsToAmount(loyaltyPointsBalance);
                    if (orderTotal > 0)
                    {
                        if (orderTotal > loyaltyPointsBalanceAmount)
                        {
                            redeemedLoyaltyPoints = loyaltyPointsBalance;
                            redeemedLoyaltyPointsAmount =
                                await _currencyService.ConvertFromPrimaryStoreCurrency(loyaltyPointsBalanceAmount,
                                    context.Currency);
                        }
                        else
                        {
                            redeemedLoyaltyPointsAmount = orderTotal;
                            redeemedLoyaltyPoints = ConvertAmountToLoyaltyPoints(
                                await _currencyService.ConvertToPrimaryStoreCurrency(redeemedLoyaltyPointsAmount,
                                    context.Currency));
                        }
                    }
                }
            }
        }

        #endregion

        orderTotal -= redeemedLoyaltyPointsAmount;
        if (_shoppingCartSettings.RoundPrices)
            orderTotal = RoundingHelper.RoundPrice(orderTotal, context.Currency);

        return (orderTotal, discountAmount, appliedDiscounts, appliedGiftVouchers, redeemedLoyaltyPoints,
            redeemedLoyaltyPointsAmount);
    }

    /// <summary>
    ///     Converts existing loyalty points to amount
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertLoyaltyPointsToAmount(int loyaltyPoints)
    {
        if (loyaltyPoints <= 0)
            return 0;

        //the amount is in the primary store currency, so it is rounded in that currency
        var result = loyaltyPoints * _loyaltyPointsSettings.ExchangeRate;
        if (_shoppingCartSettings.RoundPrices)
            result = RoundingHelper.RoundPrice(result, await _currencyService.GetPrimaryStoreCurrency());

        return result;
    }

    /// <summary>
    ///     Converts an amount to loyalty points
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <returns>Converted value</returns>
    public virtual int ConvertAmountToLoyaltyPoints(double amount)
    {
        var result = 0;
        if (amount <= 0)
            return 0;

        if (_loyaltyPointsSettings.ExchangeRate > 0)
            result = (int)Math.Ceiling(amount / _loyaltyPointsSettings.ExchangeRate);
        return result;
    }

    /// <summary>
    ///     Gets a value indicating whether a customer has minimum amount of loyalty points to use (if enabled)
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points to check</param>
    /// <returns>true - loyalty points could use; false - cannot be used.</returns>
    public virtual bool CheckMinimumLoyaltyPointsToUseRequirement(int loyaltyPoints)
    {
        if (_loyaltyPointsSettings.MinimumLoyaltyPointsToUse <= 0)
            return true;

        return loyaltyPoints >= _loyaltyPointsSettings.MinimumLoyaltyPointsToUse;
    }

    #endregion
}