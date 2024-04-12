using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
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
using Grand.Infrastructure;
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
    /// <param name="workContext">Work context</param>
    /// <param name="priceCalculationService">Price calculation service</param>
    /// <param name="taxService">Tax service</param>
    /// <param name="shippingService">Shipping service</param>
    /// <param name="paymentService">Payment service</param>
    /// <param name="checkoutAttributeParser">Checkout attribute parser</param>
    /// <param name="discountService">Discount service</param>
    /// <param name="giftVoucherService">Gift voucher service</param>
    /// <param name="loyaltyPointsService">Loyalty points service</param>
    /// <param name="productService">Product service</param>
    /// <param name="currencyService">Currency service</param>
    /// <param name="groupService">Group</param>
    /// <param name="discountValidationService">Discount validation service</param>
    /// <param name="taxSettings">Tax settings</param>
    /// <param name="loyaltyPointsSettings">Loyalty points settings</param>
    /// <param name="shippingSettings">Shipping settings</param>
    /// <param name="shoppingCartSettings">Shopping cart settings</param>
    /// <param name="catalogSettings">Catalog settings</param>
    public OrderCalculationService(IWorkContext workContext,
        IPricingService priceCalculationService,
        ITaxService taxService,
        IShippingService shippingService,
        IPaymentService paymentService,
        ICheckoutAttributeParser checkoutAttributeParser,
        IDiscountService discountService,
        IGiftVoucherService giftVoucherService,
        ILoyaltyPointsService loyaltyPointsService,
        IProductService productService,
        ICurrencyService currencyService,
        IGroupService groupService,
        IDiscountValidationService discountValidationService,
        TaxSettings taxSettings,
        LoyaltyPointsSettings loyaltyPointsSettings,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings)
    {
        _workContext = workContext;
        _pricingService = priceCalculationService;
        _taxService = taxService;
        _shippingService = shippingService;
        _paymentService = paymentService;
        _checkoutAttributeParser = checkoutAttributeParser;
        _discountService = discountService;
        _giftVoucherService = giftVoucherService;
        _loyaltyPointsService = loyaltyPointsService;
        _productService = productService;
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

    private readonly IWorkContext _workContext;
    private readonly IPricingService _pricingService;
    private readonly ITaxService _taxService;
    private readonly IShippingService _shippingService;
    private readonly IPaymentService _paymentService;
    private readonly ICheckoutAttributeParser _checkoutAttributeParser;
    private readonly IDiscountService _discountService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly ILoyaltyPointsService _loyaltyPointsService;
    private readonly IProductService _productService;
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
    ///     Gets an order discount (applied to order subtotal)
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="orderSubTotal">Order subtotal</param>
    /// <returns>Order discount</returns>
    protected virtual async Task<(double ordersubtotaldiscount, List<ApplyDiscount> appliedDiscounts)>
        GetOrderSubtotalDiscount(Customer customer, Store store, Currency currency, double orderSubTotal)
    {
        var appliedDiscounts = new List<ApplyDiscount>();
        double discountAmount = 0;
        if (_catalogSettings.IgnoreDiscounts)
            return (discountAmount, appliedDiscounts);

        var allDiscounts = await _discountService.GetActiveDiscountsByContext(DiscountType.AssignedToOrderSubTotal,
            _workContext.CurrentStore.Id, currency.CurrencyCode);
        var allowedDiscounts = new List<ApplyDiscount>();
        if (allDiscounts != null)
            foreach (var discount in allDiscounts)
            {
                var validDiscount =
                    await _discountValidationService.ValidateDiscount(discount, customer, store, currency);
                if (validDiscount.IsValid &&
                    discount.DiscountTypeId == DiscountType.AssignedToOrderSubTotal &&
                    allowedDiscounts.All(x => x.DiscountId != discount.Id))
                    allowedDiscounts.Add(new ApplyDiscount {
                        DiscountId = discount.Id,
                        IsCumulative = discount.IsCumulative,
                        CouponCode = validDiscount.CouponCode
                    });
            }

        var preferredDiscounts = await _discountService.GetPreferredDiscount(allowedDiscounts, customer,
            _workContext.WorkingCurrency, orderSubTotal);
        appliedDiscounts = preferredDiscounts.appliedDiscount;
        discountAmount = preferredDiscounts.discountAmount;

        if (discountAmount < 0)
            discountAmount = 0;

        return (discountAmount, appliedDiscounts);
    }

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
            _workContext.CurrentStore.Id, currency.CurrencyCode);
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
                        CouponCode = validDiscount.CouponCode
                    });
            }

        var (appliedDiscount, discountAmount) = await _discountService.GetPreferredDiscount(allowedDiscounts,
            customer, _workContext.WorkingCurrency, shippingTotal);
        appliedDiscounts = appliedDiscount;
        shippingDiscountAmount = discountAmount;

        if (shippingDiscountAmount < 0)
            shippingDiscountAmount = 0;

        if (_shoppingCartSettings.RoundPrices)
            shippingDiscountAmount =
                RoundingHelper.RoundPrice(shippingDiscountAmount, _workContext.WorkingCurrency);

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
            _workContext.CurrentStore.Id, currency.CurrencyCode);
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
                        CouponCode = validDiscount.CouponCode
                    });
            }

        var preferredDiscount = await _discountService.GetPreferredDiscount(allowedDiscounts, customer,
            _workContext.WorkingCurrency, orderTotal);
        appliedDiscounts = preferredDiscount.appliedDiscount;
        discountAmount = preferredDiscount.discountAmount;

        if (discountAmount < 0)
            discountAmount = 0;

        if (_shoppingCartSettings.RoundPrices)
            discountAmount = RoundingHelper.RoundPrice(discountAmount, _workContext.WorkingCurrency);

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
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    public virtual async Task<(double discountAmount, List<ApplyDiscount> appliedDiscounts,
            double subTotalWithoutDiscount, double subTotalWithDiscount, SortedDictionary<double, double> taxRates)>
        GetShoppingCartSubTotal(IList<ShoppingCartItem> cart, bool includingTax)
    {
        double discountAmount = 0;
        var appliedDiscounts = new List<ApplyDiscount>();
        double subTotalWithoutDiscount = 0;
        double subTotalWithDiscount = 0;
        var taxRates = new SortedDictionary<double, double>();

        if (!cart.Any())
            return (discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates);

        //get the customer 
        var customer = _workContext.CurrentCustomer;

        //sub totals
        double subTotalExclTaxWithoutDiscount = 0;
        double subTotalInclTaxWithoutDiscount = 0;
        foreach (var shoppingCartItem in cart)
        {
            var product = await _productService.GetProductById(shoppingCartItem.ProductId);
            if (product == null)
                continue;

            var subtotal = await _pricingService.GetSubTotal(shoppingCartItem, product);
            var sciSubTotal = subtotal.subTotal;

            var pricesExcl = await _taxService.GetProductPrice(product, sciSubTotal, false, customer);
            var sciExclTax = pricesExcl.productprice;

            var (sciInclTax, taxRate) = await _taxService.GetProductPrice(product, sciSubTotal, true, customer);

            subTotalExclTaxWithoutDiscount += sciExclTax;
            subTotalInclTaxWithoutDiscount += sciInclTax;

            //tax rates
            var sciTax = sciInclTax - sciExclTax;
            if (!(taxRate > 0) || !(sciTax > 0)) continue;
            if (!taxRates.ContainsKey(taxRate))
                taxRates.Add(taxRate, sciTax);
            else
                taxRates[taxRate] += sciTax;
        }

        //checkout attributes
        if (customer != null)
        {
            var checkoutAttributes =
                customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes,
                    _workContext.CurrentStore.Id);
            var attributeValues = await _checkoutAttributeParser.ParseCheckoutAttributeValue(checkoutAttributes);
            foreach (var attributeValue in attributeValues)
            {
                var checkoutAttributePriceExclTax =
                    await _taxService.GetCheckoutAttributePrice(attributeValue.ca, attributeValue.cav, false,
                        customer);
                var caExclTax =
                    await _currencyService.ConvertFromPrimaryStoreCurrency(
                        checkoutAttributePriceExclTax.checkoutPrice, _workContext.WorkingCurrency);

                var (checkoutPrice, taxRate) =
                    await _taxService.GetCheckoutAttributePrice(attributeValue.ca, attributeValue.cav, true,
                        customer);
                var caInclTax =
                    await _currencyService.ConvertFromPrimaryStoreCurrency(checkoutPrice,
                        _workContext.WorkingCurrency);

                subTotalExclTaxWithoutDiscount += caExclTax;
                subTotalInclTaxWithoutDiscount += caInclTax;

                //tax rates
                var caTax = caInclTax - caExclTax;
                if (!(taxRate > 0) || !(caTax > 0)) continue;
                if (!taxRates.ContainsKey(taxRate))
                    taxRates.Add(taxRate, caTax);
                else
                    taxRates[taxRate] += caTax;
            }
        }

        //subtotal without discount
        subTotalWithoutDiscount = includingTax ? subTotalInclTaxWithoutDiscount : subTotalExclTaxWithoutDiscount;

        if (subTotalWithoutDiscount < 0)
            subTotalWithoutDiscount = 0;

        if (_shoppingCartSettings.RoundPrices)
            subTotalWithoutDiscount =
                RoundingHelper.RoundPrice(subTotalWithoutDiscount, _workContext.WorkingCurrency);

        //We calculate discount amount on order subtotal excl tax (discount first)
        //calculate discount amount ('Applied to order subtotal' discount)
        var orderSubtotalDiscount = await GetOrderSubtotalDiscount(customer, _workContext.CurrentStore,
            _workContext.WorkingCurrency, subTotalExclTaxWithoutDiscount);
        var discountAmountExclTax = orderSubtotalDiscount.ordersubtotaldiscount;
        appliedDiscounts = orderSubtotalDiscount.appliedDiscounts;

        if (subTotalExclTaxWithoutDiscount < discountAmountExclTax)
            discountAmountExclTax = subTotalExclTaxWithoutDiscount;
        var discountAmountInclTax = discountAmountExclTax;
        //subtotal with discount (excl tax)
        var subTotalExclTaxWithDiscount = subTotalExclTaxWithoutDiscount - discountAmountExclTax;
        var subTotalInclTaxWithDiscount = subTotalExclTaxWithDiscount;

        //add tax for shopping items & checkout attributes
        var tempTaxRates = new Dictionary<double, double>(taxRates);
        foreach (var kvp in tempTaxRates)
        {
            var taxRate = kvp.Key;
            var taxValue = kvp.Value;

            if (taxValue == 0) continue;
            //discount the tax amount that applies to subtotal items
            if (subTotalExclTaxWithoutDiscount > 0)
            {
                var discountTax = taxRates[taxRate] * (discountAmountExclTax / subTotalExclTaxWithoutDiscount);
                discountAmountInclTax += discountTax;
                taxValue = taxRates[taxRate] - discountTax;
                if (_shoppingCartSettings.RoundPrices)
                    taxValue = RoundingHelper.RoundPrice(taxValue, _workContext.WorkingCurrency);
                taxRates[taxRate] = taxValue;
            }

            //subtotal with discount (incl tax)
            subTotalInclTaxWithDiscount += taxValue;
        }

        if (_shoppingCartSettings.RoundPrices)
        {
            discountAmountInclTax = RoundingHelper.RoundPrice(discountAmountInclTax, _workContext.WorkingCurrency);
            discountAmountExclTax = RoundingHelper.RoundPrice(discountAmountExclTax, _workContext.WorkingCurrency);
        }

        if (includingTax)
        {
            subTotalWithDiscount = subTotalInclTaxWithDiscount;
            discountAmount = discountAmountInclTax;
        }
        else
        {
            subTotalWithDiscount = subTotalExclTaxWithDiscount;
            discountAmount = discountAmountExclTax;
        }

        if (subTotalWithDiscount < 0)
            subTotalWithDiscount = 0;

        if (_shoppingCartSettings.RoundPrices)
            subTotalWithDiscount = RoundingHelper.RoundPrice(subTotalWithDiscount, _workContext.WorkingCurrency);

        return (discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates);
    }


    /// <summary>
    ///     Gets shopping cart additional shipping charge
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <returns>Additional shipping charge</returns>
    public virtual async Task<double> GetShoppingCartAdditionalShippingCharge(IList<ShoppingCartItem> cart)
    {
        var isFreeShipping = await IsFreeShipping(cart);
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
    /// <returns>A value indicating whether shipping is free</returns>
    public virtual async Task<bool> IsFreeShipping(IList<ShoppingCartItem> cart)
    {
        var customer = _workContext.CurrentCustomer;
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
            await GetShoppingCartSubTotal(cart, _shippingSettings.FreeShippingOverXIncludingTax);

        return subTotalWithDiscount > _shippingSettings.FreeShippingOverXValue;
        //otherwise, return false
    }

    /// <summary>
    ///     Adjust shipping rate (free shipping, additional charges, discounts)
    /// </summary>
    /// <param name="shippingRate">Shipping rate to adjust</param>
    /// <param name="cart">Cart</param>
    /// <returns>Adjusted shipping rate</returns>
    public virtual async Task<(double shippingRate, List<ApplyDiscount> appliedDiscounts)> AdjustShippingRate(
        double shippingRate, IList<ShoppingCartItem> cart)
    {
        var appliedDiscounts = new List<ApplyDiscount>();

        //free shipping
        if (await IsFreeShipping(cart))
            return (0, appliedDiscounts);

        //additional shipping charges
        var additionalShippingCharge = await GetShoppingCartAdditionalShippingCharge(cart);
        var adjustedRate = shippingRate + additionalShippingCharge;

        //discount
        var (discountAmount, applyDiscounts) = await GetShippingDiscount(
            _workContext.CurrentCustomer,
            _workContext.CurrentStore,
            _workContext.WorkingCurrency, adjustedRate);
        appliedDiscounts = applyDiscounts;

        adjustedRate -= discountAmount;

        if (adjustedRate < 0)
            adjustedRate = 0;

        if (_shoppingCartSettings.RoundPrices)
            adjustedRate = RoundingHelper.RoundPrice(adjustedRate, _workContext.WorkingCurrency);

        return (adjustedRate, appliedDiscounts);
    }

    /// <summary>
    ///     Gets shopping cart shipping total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <returns>Shipping total</returns>
    public virtual async
        Task<(double? shoppingCartShippingTotal, double taxRate, List<ApplyDiscount> appliedDiscounts)>
        GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart)
    {
        var includingTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
        return await GetShoppingCartShippingTotal(cart, includingTax);
    }


    /// <summary>
    ///     Gets shopping cart shipping total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    /// <returns>Shipping total</returns>
    public virtual async
        Task<(double? shoppingCartShippingTotal, double taxRate, List<ApplyDiscount> appliedDiscounts)>
        GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart, bool includingTax)
    {
        double? shippingTotal = null;
        var appliedDiscounts = new List<ApplyDiscount>();
        double taxRate = 0;

        var customer = _workContext.CurrentCustomer;
        var currency = await _currencyService.GetPrimaryExchangeRateCurrency();

        var isFreeShipping = await IsFreeShipping(cart);
        if (isFreeShipping)
            return (0, taxRate, appliedDiscounts);

        ShippingOption shippingOption = null;
        if (customer != null)
            shippingOption =
                customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption,
                    _workContext.CurrentStore.Id);

        if (shippingOption != null)
        {
            var rate = shippingOption.Rate;
            var adjustShippingRate = await AdjustShippingRate(rate, cart);
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
                await _shippingService.LoadActiveShippingRateCalculationProviders(_workContext.CurrentCustomer,
                    _workContext.CurrentStore.Id, cart);

            if (!shippingRateMethods.Any() && !_shippingSettings.AllowPickUpInStore)
                throw new GrandException("Shipping rate  method could not be loaded");

            if (shippingRateMethods.Count == 1)
            {
                var shippingRateMethod = shippingRateMethods[0];

                var shippingOptionRequest = await _shippingService.CreateShippingOptionRequests(customer, cart,
                    shippingAddress,
                    _workContext.CurrentStore);

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
                    var adjustShippingRate = await AdjustShippingRate(fixedRate.Value, cart);
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

        var shippingPrice = await _taxService.GetShippingPrice(shippingTotal.Value, includingTax, customer);
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
    /// <param name="usePaymentMethodAdditionalFee">
    ///     A value indicating whether we should use payment method additional fee when
    ///     calculating tax
    /// </param>
    /// <returns>Tax total</returns>
    public virtual async Task<(double taxtotal, SortedDictionary<double, double> taxRates)> GetTaxTotal(
        IList<ShoppingCartItem> cart, bool usePaymentMethodAdditionalFee = true)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var taxRates = new SortedDictionary<double, double>();

        var customer = _workContext.CurrentCustomer;
        var paymentMethodSystemName = "";
        if (customer != null)
            paymentMethodSystemName = customer.GetUserFieldFromEntity<string>(
                SystemCustomerFieldNames.SelectedPaymentMethod,
                _workContext.CurrentStore.Id);

        //order sub total (items + checkout attributes)
        double subTotalTaxTotal = 0;

        var shoppingCartSubTotal = await GetShoppingCartSubTotal(cart, false);
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
            var shippingTotalExcl = await GetShoppingCartShippingTotal(cart, false);
            var shippingExclTax = shippingTotalExcl.shoppingCartShippingTotal;

            var (shippingInclTax, taxRate, _) = await GetShoppingCartShippingTotal(cart, true);

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
                await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false, customer);
            var paymentMethodAdditionalFeeExclTax = additionalFeeExclTax.paymentPrice;

            var (paymentMethodAdditionalFeeInclTax, taxRate) =
                await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, true, customer);

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
            taxTotal = RoundingHelper.RoundPrice(taxTotal, _workContext.WorkingCurrency);

        return (taxTotal, taxRates);
    }


    /// <summary>
    ///     Gets shopping cart total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="useLoyaltyPoints">Use Loyalty Points</param>
    /// <param name="usePaymentMethodAdditionalFee">A value indicating whether we should use payment method additional fee</param>
    /// <returns>Shopping cart total, discount amount, applied discounts, gift vouchers, loyalty point/amount</returns>
    public virtual async Task<(double? shoppingCartTotal, double discountAmount, List<ApplyDiscount>
            appliedDiscounts, List<AppliedGiftVoucher> appliedGiftVouchers,
            int redeemedLoyaltyPoints, double redeemedLoyaltyPointsAmount)>
        GetShoppingCartTotal(IList<ShoppingCartItem> cart, bool? useLoyaltyPoints = null,
            bool usePaymentMethodAdditionalFee = true)
    {
        var redeemedLoyaltyPoints = 0;
        double redeemedLoyaltyPointsAmount = 0;

        var paymentMethodSystemName = _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(
            SystemCustomerFieldNames.SelectedPaymentMethod,
            _workContext.CurrentStore.Id);

        //subtotal without tax
        var subTotal = await GetShoppingCartSubTotal(cart, false);
        var subTotalWithDiscountBase = subTotal.subTotalWithDiscount;

        //shipping without tax
        var shippingTotal = await GetShoppingCartShippingTotal(cart, false);
        var shoppingCartShipping = shippingTotal.shoppingCartShippingTotal;

        //payment method additional fee without tax
        double paymentMethodAdditionalFeeWithoutTax = 0;
        if (usePaymentMethodAdditionalFee && !string.IsNullOrEmpty(paymentMethodSystemName))
        {
            var paymentMethodAdditionalFee =
                await _paymentService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);
            paymentMethodAdditionalFeeWithoutTax =
                (await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false,
                    _workContext.CurrentCustomer))
                .paymentPrice;
        }

        //tax
        var shoppingCartTax = (await GetTaxTotal(cart, usePaymentMethodAdditionalFee)).taxtotal;

        //order total
        double resultTemp = 0;
        resultTemp += subTotalWithDiscountBase;
        if (shoppingCartShipping.HasValue) resultTemp += shoppingCartShipping.Value;

        resultTemp += paymentMethodAdditionalFeeWithoutTax;
        resultTemp += shoppingCartTax;
        if (_shoppingCartSettings.RoundPrices)
            resultTemp = RoundingHelper.RoundPrice(resultTemp, _workContext.WorkingCurrency);

        #region Order total discount

        var totalDiscount = await GetOrderTotalDiscount(_workContext.CurrentCustomer, _workContext.CurrentStore,
            _workContext.WorkingCurrency, resultTemp);
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
            resultTemp = RoundingHelper.RoundPrice(resultTemp, _workContext.WorkingCurrency);

        #endregion

        #region Applied gift vouchers

        var appliedGiftVouchers = new List<AppliedGiftVoucher>();
        //we don't apply gift vouchers for recurring products
        var giftVouchers =
            await GetActiveGiftVouchers(_workContext.CurrentCustomer, _workContext.WorkingCurrency,
                _workContext.CurrentStore);
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
            resultTemp = RoundingHelper.RoundPrice(resultTemp, _workContext.WorkingCurrency);

        if (!shoppingCartShipping.HasValue)
            //we have errors
            return (null, discountAmount, appliedDiscounts, appliedGiftVouchers, redeemedLoyaltyPoints,
                redeemedLoyaltyPointsAmount);

        var orderTotal = resultTemp;

        #region Loyalty points

        if (_loyaltyPointsSettings.Enabled)
        {
            useLoyaltyPoints ??= _workContext.CurrentCustomer.GetUserFieldFromEntity<bool>(
                SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout,
                _workContext.CurrentStore.Id);

            if (useLoyaltyPoints.Value)
            {
                var loyaltyPointsBalance =
                    await _loyaltyPointsService.GetLoyaltyPointsBalance(_workContext.CurrentCustomer.Id,
                        _workContext.CurrentStore.Id);
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
                                    _workContext.WorkingCurrency);
                        }
                        else
                        {
                            redeemedLoyaltyPointsAmount = orderTotal;
                            redeemedLoyaltyPoints = ConvertAmountToLoyaltyPoints(
                                await _currencyService.ConvertToPrimaryStoreCurrency(redeemedLoyaltyPointsAmount,
                                    _workContext.WorkingCurrency));
                        }
                    }
                }
            }
        }

        #endregion

        orderTotal -= redeemedLoyaltyPointsAmount;
        if (_shoppingCartSettings.RoundPrices)
            orderTotal = RoundingHelper.RoundPrice(orderTotal, _workContext.WorkingCurrency);

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

        var result = loyaltyPoints * _loyaltyPointsSettings.ExchangeRate;
        if (_shoppingCartSettings.RoundPrices) result = RoundingHelper.RoundPrice(result, _workContext.WorkingCurrency);

        return await Task.FromResult(result);
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