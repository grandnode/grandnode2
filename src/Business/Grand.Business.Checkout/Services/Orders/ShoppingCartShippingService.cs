using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
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
using Grand.SharedKernel;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Shopping cart shipping calculation
/// </summary>
public class ShoppingCartShippingService : IShoppingCartShippingService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="shoppingCartSubTotalService">Shopping cart subtotal service</param>
    /// <param name="taxService">Tax service</param>
    /// <param name="shippingService">Shipping service</param>
    /// <param name="discountService">Discount service</param>
    /// <param name="discountValidationService">Discount validation service</param>
    /// <param name="currencyService">Currency service</param>
    /// <param name="groupService">Group</param>
    /// <param name="shippingSettings">Shipping settings</param>
    /// <param name="shoppingCartSettings">Shopping cart settings</param>
    /// <param name="catalogSettings">Catalog settings</param>
    public ShoppingCartShippingService(IShoppingCartSubTotalService shoppingCartSubTotalService,
        ITaxService taxService,
        IShippingService shippingService,
        IDiscountService discountService,
        IDiscountValidationService discountValidationService,
        ICurrencyService currencyService,
        IGroupService groupService,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings)
    {
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _taxService = taxService;
        _shippingService = shippingService;
        _discountService = discountService;
        _discountValidationService = discountValidationService;
        _currencyService = currencyService;
        _groupService = groupService;
        _shippingSettings = shippingSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Fields

    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;
    private readonly ITaxService _taxService;
    private readonly IShippingService _shippingService;
    private readonly IDiscountService _discountService;
    private readonly IDiscountValidationService _discountValidationService;
    private readonly ICurrencyService _currencyService;
    private readonly IGroupService _groupService;
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

    #endregion

    #region Methods

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
        var subTotal = await _shoppingCartSubTotalService.GetShoppingCartSubTotal(cart, context, _shippingSettings.FreeShippingOverXIncludingTax);

        return subTotal.SubTotalWithDiscount > _shippingSettings.FreeShippingOverXValue;
        //otherwise, return false
    }

    /// <summary>
    ///     Adjust shipping rate (free shipping, additional charges, discounts)
    /// </summary>
    /// <param name="shippingRate">Shipping rate to adjust</param>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Adjusted shipping rate</returns>
    public virtual async Task<ShippingRateAdjustment> AdjustShippingRate(
        double shippingRate, IList<ShoppingCartItem> cart, CalculationContext context)
    {
        var appliedDiscounts = new List<ApplyDiscount>();

        //free shipping
        if (await IsFreeShipping(cart, context))
            return new ShippingRateAdjustment(0, appliedDiscounts);

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

        return new ShippingRateAdjustment(adjustedRate, appliedDiscounts);
    }

    /// <summary>
    ///     Gets shopping cart shipping total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Shipping total</returns>
    public virtual async Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart,
        CalculationContext context)
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
    public virtual async Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart,
        CalculationContext context, bool includingTax)
    {
        double? shippingTotal = null;
        var appliedDiscounts = new List<ApplyDiscount>();
        double taxRate = 0;

        var customer = context.Customer;
        var store = context.Store;
        var currency = await _currencyService.GetPrimaryExchangeRateCurrency();

        var isFreeShipping = await IsFreeShipping(cart, context);
        if (isFreeShipping)
            return new ShoppingCartShippingTotal(0, taxRate, appliedDiscounts);

        ShippingOption shippingOption = null;
        if (customer != null)
            shippingOption =
                customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption,
                    context.Store.Id);

        if (shippingOption != null)
        {
            var rate = shippingOption.Rate;
            var adjustShippingRate = await AdjustShippingRate(rate, cart, context);
            shippingTotal = adjustShippingRate.ShippingRate;
            appliedDiscounts = adjustShippingRate.AppliedDiscounts;
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
                    shippingTotal = adjustShippingRate.ShippingRate;
                    appliedDiscounts = adjustShippingRate.AppliedDiscounts;
                }
            }
        }

        switch (shippingTotal)
        {
            case null:
                return new ShoppingCartShippingTotal(null, taxRate, appliedDiscounts);
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

        return new ShoppingCartShippingTotal(shippingTotalTaxed, taxRate, appliedDiscounts);
    }

    #endregion
}
