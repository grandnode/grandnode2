using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Orders
{
    /// <summary>
    /// Order calc service
    /// </summary>
    public partial class OrderCalculationService : IOrderCalculationService
    {
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
        private readonly TaxSettings _taxSettings;
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
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
            _taxSettings = taxSettings;
            _loyaltyPointsSettings = loyaltyPointsSettings;
            _shippingSettings = shippingSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets an order discount (applied to order subtotal)
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="orderSubTotal">Order subtotal</param>
        /// <returns>Order discount</returns>
        protected virtual async Task<(decimal ordersubtotaldiscount, List<ApplyDiscount> appliedDiscounts)> GetOrderSubtotalDiscount(Customer customer, Currency currency, decimal orderSubTotal)
        {
            var appliedDiscounts = new List<ApplyDiscount>();
            decimal discountAmount = decimal.Zero;
            if (_catalogSettings.IgnoreDiscounts)
                return (discountAmount, appliedDiscounts);

            var allDiscounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToOrderSubTotal, storeId: _workContext.CurrentStore.Id, currencyCode: currency.CurrencyCode);
            var allowedDiscounts = new List<ApplyDiscount>();
            if (allDiscounts != null)
                foreach (var discount in allDiscounts)
                {
                    var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                    if (validDiscount.IsValid &&
                        discount.DiscountTypeId == DiscountType.AssignedToOrderSubTotal &&
                        !allowedDiscounts.Where(x => x.DiscountId == discount.Id).Any())
                    {
                        allowedDiscounts.Add(new ApplyDiscount
                        {
                            DiscountId = discount.Id,
                            IsCumulative = discount.IsCumulative,
                            CouponCode = validDiscount.CouponCode,
                        });
                    }
                }

            var preferredDiscounts = await _discountService.GetPreferredDiscount(allowedDiscounts, customer, _workContext.WorkingCurrency, orderSubTotal);
            appliedDiscounts = preferredDiscounts.appliedDiscount;
            discountAmount = preferredDiscounts.discountAmount;

            if (discountAmount < decimal.Zero)
                discountAmount = decimal.Zero;

            return (discountAmount, appliedDiscounts);
        }

        /// <summary>
        /// Gets a shipping discount
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="shippingTotal">Shipping total</param>
        /// <returns>Shipping discount</returns>
        protected virtual async Task<(decimal shippingDiscount, List<ApplyDiscount> appliedDiscounts)> GetShippingDiscount(Customer customer, Currency currency, decimal shippingTotal)
        {
            var appliedDiscounts = new List<ApplyDiscount>();
            decimal shippingDiscountAmount = decimal.Zero;
            if (_catalogSettings.IgnoreDiscounts)
                return (shippingDiscountAmount, appliedDiscounts);

            var allDiscounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToShipping, storeId: _workContext.CurrentStore.Id, currencyCode: currency.CurrencyCode);
            var allowedDiscounts = new List<ApplyDiscount>();
            if (allDiscounts != null)
                foreach (var discount in allDiscounts)
                {
                    var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                    if (validDiscount.IsValid &&
                        discount.DiscountTypeId == DiscountType.AssignedToShipping &&
                        !allowedDiscounts.Where(x => x.DiscountId == discount.Id).Any())
                    {
                        allowedDiscounts.Add(new ApplyDiscount
                        {
                            DiscountId = discount.Id,
                            IsCumulative = discount.IsCumulative,
                            CouponCode = validDiscount.CouponCode,
                        });
                    }
                }

            var (appliedDiscount, discountAmount) = await _discountService.GetPreferredDiscount(allowedDiscounts, customer, _workContext.WorkingCurrency, shippingTotal);
            appliedDiscounts = appliedDiscount;
            shippingDiscountAmount = discountAmount;

            if (shippingDiscountAmount < decimal.Zero)
                shippingDiscountAmount = decimal.Zero;

            if (_shoppingCartSettings.RoundPrices)
            {
                shippingDiscountAmount = RoundingHelper.RoundPrice(shippingDiscountAmount, _workContext.WorkingCurrency);
            }
            return (shippingDiscountAmount, appliedDiscounts);
        }

        /// <summary>
        /// Gets an order discount (applied to order total)
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="orderTotal">Order total</param>
        /// <returns>Order discount</returns>
        protected virtual async Task<(decimal orderTotalDiscount, List<ApplyDiscount> appliedDiscounts)> GetOrderTotalDiscount(Customer customer, Currency currency, decimal orderTotal)
        {
            var appliedDiscounts = new List<ApplyDiscount>();
            decimal discountAmount = decimal.Zero;
            if (_catalogSettings.IgnoreDiscounts)
                return (discountAmount, appliedDiscounts);

            var allDiscounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToOrderTotal, storeId: _workContext.CurrentStore.Id, currencyCode: currency.CurrencyCode);
            var allowedDiscounts = new List<ApplyDiscount>();
            if (allDiscounts != null)
                foreach (var discount in allDiscounts)
                {
                    var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                    if (validDiscount.IsValid &&
                               discount.DiscountTypeId == DiscountType.AssignedToOrderTotal &&
                               !allowedDiscounts.Where(x => x.DiscountId == discount.Id).Any())
                    {
                        allowedDiscounts.Add(new ApplyDiscount
                        {
                            DiscountId = discount.Id,
                            IsCumulative = discount.IsCumulative,
                            CouponCode = validDiscount.CouponCode,
                        });
                    }
                }
            var preferredDiscount = await _discountService.GetPreferredDiscount(allowedDiscounts, customer, _workContext.WorkingCurrency, orderTotal);
            appliedDiscounts = preferredDiscount.appliedDiscount;
            discountAmount = preferredDiscount.discountAmount;

            if (discountAmount < decimal.Zero)
                discountAmount = decimal.Zero;

            if (_shoppingCartSettings.RoundPrices)
            {
                discountAmount = RoundingHelper.RoundPrice(discountAmount, _workContext.WorkingCurrency);
            }
            return (discountAmount, appliedDiscounts);
        }

        /// <summary>
        /// Get active gift vouchers that are applied by a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Active gift vouchers</returns>
        private async Task<IList<GiftVoucher>> GetActiveGiftVouchers(Customer customer, Currency currency)
        {
            var result = new List<GiftVoucher>();
            if (customer == null)
                return result;

            string[] couponCodes = customer.ParseAppliedCouponCodes(SystemCustomerFieldNames.GiftVoucherCoupons);
            foreach (var couponCode in couponCodes)
            {
                var giftVouchers = await _giftVoucherService.GetAllGiftVouchers(isGiftVoucherActivated: true, giftVoucherCouponCode: couponCode);
                foreach (var gc in giftVouchers)
                {
                    if (gc.IsGiftVoucherValid(currency))
                        result.Add(gc);
                }
            }

            return result;
        }

        #endregion

        #region Methods


        /// <summary>
        /// Gets shopping cart subtotal
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <param name="subTotalWithoutDiscount">Sub total (without discount)</param>
        /// <param name="subTotalWithDiscount">Sub total (with discount)</param>
        /// <param name="taxRates">Tax rates (of order sub total)</param>
        public virtual async Task<(decimal discountAmount, List<ApplyDiscount> appliedDiscounts, 
            decimal subTotalWithoutDiscount, decimal subTotalWithDiscount, SortedDictionary<decimal, decimal> taxRates)>
            GetShoppingCartSubTotal(IList<ShoppingCartItem> cart, bool includingTax)
        {
            var discountAmount = decimal.Zero;
            var appliedDiscounts = new List<ApplyDiscount>();
            var subTotalWithoutDiscount = decimal.Zero;
            var subTotalWithDiscount = decimal.Zero;
            var taxRates = new SortedDictionary<decimal, decimal>();

            if (!cart.Any())
                return (discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates);

            //get the customer 
            Customer customer = _workContext.CurrentCustomer;
            
            //sub totals
            decimal subTotalExclTaxWithoutDiscount = decimal.Zero;
            decimal subTotalInclTaxWithoutDiscount = decimal.Zero;
            foreach (var shoppingCartItem in cart)
            {
                var product = await _productService.GetProductById(shoppingCartItem.ProductId);
                if (product == null)
                    continue;

                var subtotal = await _pricingService.GetSubTotal(shoppingCartItem, product);
                decimal sciSubTotal = subtotal.subTotal;

                decimal taxRate;
                var pricesExcl = await _taxService.GetProductPrice(product, sciSubTotal, false, customer);
                decimal sciExclTax = pricesExcl.productprice;

                var pricesIncl = await _taxService.GetProductPrice(product, sciSubTotal, true, customer);
                decimal sciInclTax = pricesIncl.productprice;
                taxRate = pricesIncl.taxRate;

                subTotalExclTaxWithoutDiscount += sciExclTax;
                subTotalInclTaxWithoutDiscount += sciInclTax;

                //tax rates
                decimal sciTax = sciInclTax - sciExclTax;
                if (taxRate > decimal.Zero && sciTax > decimal.Zero)
                {
                    if (!taxRates.ContainsKey(taxRate))
                    {
                        taxRates.Add(taxRate, sciTax);
                    }
                    else
                    {
                        taxRates[taxRate] = taxRates[taxRate] + sciTax;
                    }
                }
            }

            //checkout attributes
            if (customer != null)
            {
                var checkoutAttributes = customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, _workContext.CurrentStore.Id);
                var attributeValues = await _checkoutAttributeParser.ParseCheckoutAttributeValue(checkoutAttributes);
                foreach (var attributeValue in attributeValues)
                {
                    decimal taxRate;
                    var checkoutAttributePriceExclTax = await _taxService.GetCheckoutAttributePrice(attributeValue.ca, attributeValue.cav, false, customer);
                    decimal caExclTax = await _currencyService.ConvertFromPrimaryStoreCurrency(checkoutAttributePriceExclTax.checkoutPrice, _workContext.WorkingCurrency);

                    var checkoutAttributePriceInclTax = await _taxService.GetCheckoutAttributePrice(attributeValue.ca, attributeValue.cav, true, customer);
                    decimal caInclTax = await _currencyService.ConvertFromPrimaryStoreCurrency(checkoutAttributePriceInclTax.checkoutPrice, _workContext.WorkingCurrency);

                    taxRate = checkoutAttributePriceInclTax.taxRate;

                    subTotalExclTaxWithoutDiscount += caExclTax;
                    subTotalInclTaxWithoutDiscount += caInclTax;

                    //tax rates
                    decimal caTax = caInclTax - caExclTax;
                    if (taxRate > decimal.Zero && caTax > decimal.Zero)
                    {
                        if (!taxRates.ContainsKey(taxRate))
                        {
                            taxRates.Add(taxRate, caTax);
                        }
                        else
                        {
                            taxRates[taxRate] = taxRates[taxRate] + caTax;
                        }
                    }
                }
            }

            //subtotal without discount
            subTotalWithoutDiscount = includingTax ? subTotalInclTaxWithoutDiscount : subTotalExclTaxWithoutDiscount;

            if (subTotalWithoutDiscount < decimal.Zero)
                subTotalWithoutDiscount = decimal.Zero;

            if (_shoppingCartSettings.RoundPrices)
            {
                subTotalWithoutDiscount = RoundingHelper.RoundPrice(subTotalWithoutDiscount, _workContext.WorkingCurrency);
            }
            //We calculate discount amount on order subtotal excl tax (discount first)
            //calculate discount amount ('Applied to order subtotal' discount)
            var orderSubtotalDiscount = await GetOrderSubtotalDiscount(customer, _workContext.WorkingCurrency, subTotalExclTaxWithoutDiscount);
            decimal discountAmountExclTax = orderSubtotalDiscount.ordersubtotaldiscount;
            appliedDiscounts = orderSubtotalDiscount.appliedDiscounts;

            if (subTotalExclTaxWithoutDiscount < discountAmountExclTax)
                discountAmountExclTax = subTotalExclTaxWithoutDiscount;
            decimal discountAmountInclTax = discountAmountExclTax;
            //subtotal with discount (excl tax)
            decimal subTotalExclTaxWithDiscount = subTotalExclTaxWithoutDiscount - discountAmountExclTax;
            decimal subTotalInclTaxWithDiscount = subTotalExclTaxWithDiscount;

            //add tax for shopping items & checkout attributes
            var tempTaxRates = new Dictionary<decimal, decimal>(taxRates);
            foreach (KeyValuePair<decimal, decimal> kvp in tempTaxRates)
            {
                decimal taxRate = kvp.Key;
                decimal taxValue = kvp.Value;

                if (taxValue != decimal.Zero)
                {
                    //discount the tax amount that applies to subtotal items
                    if (subTotalExclTaxWithoutDiscount > decimal.Zero)
                    {
                        decimal discountTax = taxRates[taxRate] * (discountAmountExclTax / subTotalExclTaxWithoutDiscount);
                        discountAmountInclTax += discountTax;
                        taxValue = taxRates[taxRate] - discountTax;
                        if (_shoppingCartSettings.RoundPrices)
                            taxValue = RoundingHelper.RoundPrice(taxValue, _workContext.WorkingCurrency);
                        taxRates[taxRate] = taxValue;
                    }

                    //subtotal with discount (incl tax)
                    subTotalInclTaxWithDiscount += taxValue;
                }
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

            if (subTotalWithDiscount < decimal.Zero)
                subTotalWithDiscount = decimal.Zero;

            if (_shoppingCartSettings.RoundPrices)
                subTotalWithDiscount = RoundingHelper.RoundPrice(subTotalWithDiscount, _workContext.WorkingCurrency);

            return (discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates);
        }


        /// <summary>
        /// Gets shopping cart additional shipping charge
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>Additional shipping charge</returns>
        public virtual async Task<decimal> GetShoppingCartAdditionalShippingCharge(IList<ShoppingCartItem> cart)
        {
            decimal additionalShippingCharge = decimal.Zero;

            bool isFreeShipping = await IsFreeShipping(cart);
            if (isFreeShipping)
                return decimal.Zero;

            foreach (var sci in cart)
                if (sci.IsShipEnabled && !sci.IsFreeShipping)
                    additionalShippingCharge += sci.AdditionalShippingCharge;

            return additionalShippingCharge;
        }

        /// <summary>
        /// Gets a value indicating whether shipping is free
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>A value indicating whether shipping is free</returns>
        public virtual async Task<bool> IsFreeShipping(IList<ShoppingCartItem> cart)
        {
            Customer customer = _workContext.CurrentCustomer;
            if (customer != null)
            {
                //check whether customer has a free shipping
                if (customer.FreeShipping)
                    return true;

                //check whether customer is in a customer group with free shipping applied
                var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
                foreach (var customerGroup in customerGroups)
                    if (customerGroup.FreeShipping)
                        return true;

            }

            bool shoppingCartRequiresShipping = cart.RequiresShipping();
            if (!shoppingCartRequiresShipping)
                return true;

            //check whether all shopping cart items are marked as free shipping
            bool allItemsAreFreeShipping = true;
            foreach (var sc in cart)
            {
                if (sc.IsShipEnabled && !sc.IsFreeShipping)
                {
                    allItemsAreFreeShipping = false;
                    break;
                }
            }
            if (allItemsAreFreeShipping)
                return true;

            //free shipping over $X
            if (_shippingSettings.FreeShippingOverXEnabled)
            {
                //check whether we have subtotal enough to have free shipping
                var (discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates) = await GetShoppingCartSubTotal(cart, _shippingSettings.FreeShippingOverXIncludingTax);
                var subTotalDiscountAmount = discountAmount;
                var subTotalAppliedDiscounts = appliedDiscounts;
                var subTotalWithoutDiscountBase = subTotalWithoutDiscount;
                var subTotalWithDiscountBase = subTotalWithDiscount;

                if (subTotalWithDiscountBase > _shippingSettings.FreeShippingOverXValue)
                    return true;
            }

            //otherwise, return false
            return false;
        }

        /// <summary>
        /// Adjust shipping rate (free shipping, additional charges, discounts)
        /// </summary>
        /// <param name="shippingRate">Shipping rate to adjust</param>
        /// <param name="cart">Cart</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Adjusted shipping rate</returns>
        public virtual async Task<(decimal shippingRate, List<ApplyDiscount> appliedDiscounts)> AdjustShippingRate(decimal shippingRate, IList<ShoppingCartItem> cart)
        {
            var appliedDiscounts = new List<ApplyDiscount>();

            //free shipping
            if (await IsFreeShipping(cart))
                return (decimal.Zero, appliedDiscounts);

            //additional shipping charges
            decimal additionalShippingCharge = await GetShoppingCartAdditionalShippingCharge(cart);
            var adjustedRate = shippingRate + additionalShippingCharge;

            //discount
            var customer = _workContext.CurrentCustomer;
            var shippingDiscount = await GetShippingDiscount(customer, _workContext.WorkingCurrency, adjustedRate);
            decimal discountAmount = shippingDiscount.shippingDiscount;
            appliedDiscounts = shippingDiscount.appliedDiscounts;

            adjustedRate -= discountAmount;

            if (adjustedRate < decimal.Zero)
                adjustedRate = decimal.Zero;

            if (_shoppingCartSettings.RoundPrices)
            {
                adjustedRate = RoundingHelper.RoundPrice(adjustedRate, _workContext.WorkingCurrency);
            }

            return (adjustedRate, appliedDiscounts);
        }

        /// <summary>
        /// Gets shopping cart shipping total
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>Shipping total</returns>
        public virtual async Task<(decimal? shoppingCartShippingTotal, decimal taxRate, List<ApplyDiscount> appliedDiscounts)> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart)
        {
            bool includingTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            return await GetShoppingCartShippingTotal(cart, includingTax);
        }


        /// <summary>
        /// Gets shopping cart shipping total
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="taxRate">Applied tax rate</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Shipping total</returns>
        public virtual async Task<(decimal? shoppingCartShippingTotal, decimal taxRate, List<ApplyDiscount> appliedDiscounts)> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart, bool includingTax)
        {
            decimal? shippingTotal = null;
            decimal? shippingTotalTaxed = null;
            var appliedDiscounts = new List<ApplyDiscount>();
            var taxRate = decimal.Zero;

            var customer = _workContext.CurrentCustomer;
            var currency = await _currencyService.GetPrimaryExchangeRateCurrency();

            bool isFreeShipping = await IsFreeShipping(cart);
            if (isFreeShipping)
                return (decimal.Zero, taxRate, appliedDiscounts);

            ShippingOption shippingOption = null;
            if (customer != null)
                shippingOption = customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption, _workContext.CurrentStore.Id);

            if (shippingOption != null)
            {
                var rate = shippingOption.Rate;
                var adjustshipingRate = await AdjustShippingRate(rate, cart);
                shippingTotal = adjustshipingRate.shippingRate;
                appliedDiscounts = adjustshipingRate.appliedDiscounts;
            }
            else
            {
                //use fixed rate (if possible)
                Address shippingAddress = null;
                if (customer != null)
                    shippingAddress = customer.ShippingAddress;

                var shippingRateMethods = await _shippingService.LoadActiveShippingRateCalculationProviders(_workContext.CurrentCustomer, _workContext.CurrentStore.Id, cart);

                if (!shippingRateMethods.Any() && !_shippingSettings.AllowPickUpInStore)
                    throw new GrandException("Shipping rate  method could not be loaded");

                if (shippingRateMethods.Count == 1)
                {
                    var shippingRateMethod = shippingRateMethods[0];

                    var shippingOptionRequests = await _shippingService.CreateShippingOptionRequests(customer, cart,
                        shippingAddress,
                        _workContext.CurrentStore);
                    
                    decimal? fixedRate = null;
                    foreach (var shippingOptionRequest in shippingOptionRequests)
                    {
                        //calculate fixed rates for each request-package
                        var fixedRateTmp = await shippingRateMethod.GetFixedRate(shippingOptionRequest);
                        if (fixedRateTmp.HasValue)
                        {
                            if (!fixedRate.HasValue)
                                fixedRate = decimal.Zero;

                            fixedRate += fixedRateTmp.Value;
                        }
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

            if (shippingTotal.HasValue)
            {
                if (shippingTotal.Value < decimal.Zero)
                    shippingTotal = decimal.Zero;

                //round
                if (_shoppingCartSettings.RoundPrices)
                    shippingTotal = RoundingHelper.RoundPrice(shippingTotal.Value, currency);

                var shippingPrice = await _taxService.GetShippingPrice(shippingTotal.Value, includingTax, customer);
                shippingTotalTaxed = shippingPrice.shippingPrice;
                taxRate = shippingPrice.taxRate;

                //round
                if (_shoppingCartSettings.RoundPrices)
                    shippingTotalTaxed = RoundingHelper.RoundPrice(shippingTotalTaxed.Value, currency);
            }

            return (shippingTotalTaxed, taxRate, appliedDiscounts);
        }

        /// <summary>
        /// Gets tax
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <param name="taxRates">Tax rates</param>
        /// <param name="usePaymentMethodAdditionalFee">A value indicating whether we should use payment method additional fee when calculating tax</param>
        /// <returns>Tax total</returns>
        public virtual async Task<(decimal taxtotal, SortedDictionary<decimal, decimal> taxRates)> GetTaxTotal(IList<ShoppingCartItem> cart, bool usePaymentMethodAdditionalFee = true)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            var taxRates = new SortedDictionary<decimal, decimal>();

            var customer = _workContext.CurrentCustomer;
            string paymentMethodSystemName = "";
            if (customer != null)
            {
                paymentMethodSystemName = customer.GetUserFieldFromEntity<string>(
                    SystemCustomerFieldNames.SelectedPaymentMethod,
                    _workContext.CurrentStore.Id);
            }

            //order sub total (items + checkout attributes)
            decimal subTotalTaxTotal = decimal.Zero;

            var shoppingCartSubTotal = await GetShoppingCartSubTotal(cart, false);
            SortedDictionary<decimal, decimal> orderSubTotalTaxRates = shoppingCartSubTotal.taxRates;

            foreach (KeyValuePair<decimal, decimal> kvp in orderSubTotalTaxRates)
            {
                decimal taxRate = kvp.Key;
                decimal taxValue = kvp.Value;
                subTotalTaxTotal += taxValue;

                if (taxRate > decimal.Zero && taxValue > decimal.Zero)
                {
                    if (!taxRates.ContainsKey(taxRate))
                        taxRates.Add(taxRate, taxValue);
                    else
                        taxRates[taxRate] = taxRates[taxRate] + taxValue;
                }
            }

            //shipping
            decimal shippingTax = decimal.Zero;
            if (_taxSettings.ShippingIsTaxable)
            {
                decimal taxRate;
                var shippingTotalExcl = await GetShoppingCartShippingTotal(cart, false);
                decimal? shippingExclTax = shippingTotalExcl.shoppingCartShippingTotal;

                var shippingTotalIncl = await GetShoppingCartShippingTotal(cart, true);
                decimal? shippingInclTax = shippingTotalIncl.shoppingCartShippingTotal;

                taxRate = shippingTotalIncl.taxRate;

                if (shippingExclTax.HasValue && shippingInclTax.HasValue)
                {
                    shippingTax = shippingInclTax.Value - shippingExclTax.Value;
                    //ensure that tax is equal or greater than zero
                    if (shippingTax < decimal.Zero)
                        shippingTax = decimal.Zero;

                    //tax rates
                    if (taxRate > decimal.Zero && shippingTax > decimal.Zero)
                    {
                        if (!taxRates.ContainsKey(taxRate))
                            taxRates.Add(taxRate, shippingTax);
                        else
                            taxRates[taxRate] = taxRates[taxRate] + shippingTax;
                    }
                }
            }

            //payment method additional fee
            decimal paymentMethodAdditionalFeeTax = decimal.Zero;
            if (usePaymentMethodAdditionalFee && _taxSettings.PaymentMethodAdditionalFeeIsTaxable)
            {
                decimal taxRate;
                decimal paymentMethodAdditionalFee = await _paymentService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);

                var additionalFeeExclTax = await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false, customer);
                decimal paymentMethodAdditionalFeeExclTax = additionalFeeExclTax.paymentPrice;

                var additionalFeeInclTax = await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, true, customer);
                decimal paymentMethodAdditionalFeeInclTax = additionalFeeInclTax.paymentPrice;

                taxRate = additionalFeeInclTax.taxRate;

                paymentMethodAdditionalFeeTax = paymentMethodAdditionalFeeInclTax - paymentMethodAdditionalFeeExclTax;
                //ensure that tax is equal or greater than zero
                if (paymentMethodAdditionalFeeTax < decimal.Zero)
                    paymentMethodAdditionalFeeTax = decimal.Zero;

                //tax rates
                if (taxRate > decimal.Zero && paymentMethodAdditionalFeeTax > decimal.Zero)
                {
                    if (!taxRates.ContainsKey(taxRate))
                        taxRates.Add(taxRate, paymentMethodAdditionalFeeTax);
                    else
                        taxRates[taxRate] = taxRates[taxRate] + paymentMethodAdditionalFeeTax;
                }
            }

            //add at least one tax rate (0%)
            if (!taxRates.Any())
                taxRates.Add(decimal.Zero, decimal.Zero);

            //summarize taxes
            decimal taxTotal = subTotalTaxTotal + shippingTax + paymentMethodAdditionalFeeTax;
            //ensure that tax is equal or greater than zero
            if (taxTotal < decimal.Zero)
                taxTotal = decimal.Zero;
            //round tax
            if (_shoppingCartSettings.RoundPrices)
            {
                taxTotal = RoundingHelper.RoundPrice(taxTotal, _workContext.WorkingCurrency);
            }
            return (taxTotal, taxRates);
        }


        /// <summary>
        /// Gets shopping cart total
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="ignoreLoyaltyPonts">A value indicating whether we should ignore loyalty points</param>
        /// <param name="usePaymentMethodAdditionalFee">A value indicating whether we should use payment method additional fee</param>
        /// <returns>Shopping cart total, discount amount, applied discounts, gift vouchers, loyalty point/amount</returns>
        public virtual async Task<(decimal? shoppingCartTotal, decimal discountAmount, List<ApplyDiscount> appliedDiscounts, List<AppliedGiftVoucher> appliedGiftVouchers,
            int redeemedLoyaltyPoints, decimal redeemedLoyaltyPointsAmount)>
            GetShoppingCartTotal(IList<ShoppingCartItem> cart, bool? useLoyaltyPoints = null, bool usePaymentMethodAdditionalFee = true)
        {

            var redeemedLoyaltyPoints = 0;
            var redeemedLoyaltyPointsAmount = decimal.Zero;

            var customer = _workContext.CurrentCustomer;

            string paymentMethodSystemName = "";
            if (customer != null)
            {
                paymentMethodSystemName = customer.GetUserFieldFromEntity<string>(
                    SystemCustomerFieldNames.SelectedPaymentMethod,
                    _workContext.CurrentStore.Id);
            }

            //subtotal without tax
            var subTotal = await GetShoppingCartSubTotal(cart, false);
            decimal subTotalWithDiscountBase = subTotal.subTotalWithDiscount;

            //subtotal with discount
            decimal subtotalBase = subTotalWithDiscountBase;

            //shipping without tax
            var shippingTotal = await GetShoppingCartShippingTotal(cart, false);
            decimal? shoppingCartShipping = shippingTotal.shoppingCartShippingTotal;

            //payment method additional fee without tax
            decimal paymentMethodAdditionalFeeWithoutTax = decimal.Zero;
            if (usePaymentMethodAdditionalFee && !string.IsNullOrEmpty(paymentMethodSystemName))
            {
                var paymentMethodAdditionalFee = await _paymentService.GetAdditionalHandlingFee(cart, paymentMethodSystemName);
                paymentMethodAdditionalFeeWithoutTax = (await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false, customer)).paymentPrice;
            }

            //tax
            decimal shoppingCartTax = (await GetTaxTotal(cart, usePaymentMethodAdditionalFee)).taxtotal;

            //order total
            decimal resultTemp = decimal.Zero;
            resultTemp += subtotalBase;
            if (shoppingCartShipping.HasValue)
            {
                resultTemp += shoppingCartShipping.Value;
            }
            resultTemp += paymentMethodAdditionalFeeWithoutTax;
            resultTemp += shoppingCartTax;
            if (_shoppingCartSettings.RoundPrices)
            {
                resultTemp = RoundingHelper.RoundPrice(resultTemp, _workContext.WorkingCurrency);
            }
            #region Order total discount

            var totalDiscount = await GetOrderTotalDiscount(customer, _workContext.WorkingCurrency, resultTemp);
            var discountAmount = totalDiscount.orderTotalDiscount;
            var appliedDiscounts = totalDiscount.appliedDiscounts;

            //sub totals with discount        
            if (resultTemp < discountAmount)
                discountAmount = resultTemp;

            //reduce subtotal
            resultTemp -= discountAmount;

            if (resultTemp < decimal.Zero)
                resultTemp = decimal.Zero;
            if (_shoppingCartSettings.RoundPrices)
                resultTemp = RoundingHelper.RoundPrice(resultTemp, _workContext.WorkingCurrency);

            #endregion

            #region Applied gift vouchers

            var appliedGiftVouchers = new List<AppliedGiftVoucher>();
            //we don't apply gift vouchers for recurring products
            var giftVouchers = await GetActiveGiftVouchers(customer, _workContext.WorkingCurrency);
            if (giftVouchers != null)
                foreach (var gc in giftVouchers)
                    if (resultTemp > decimal.Zero)
                    {
                        decimal remainingAmount = gc.GetGiftVoucherRemainingAmount();
                        decimal amountCanBeUsed = resultTemp > remainingAmount ?
                            remainingAmount :
                            resultTemp;

                        //reduce subtotal
                        resultTemp -= amountCanBeUsed;

                        var appliedGiftVoucher = new AppliedGiftVoucher
                        {
                            GiftVoucher = gc,
                            AmountCanBeUsed = amountCanBeUsed
                        };
                        appliedGiftVouchers.Add(appliedGiftVoucher);
                    }

            #endregion

            if (resultTemp < decimal.Zero)
                resultTemp = decimal.Zero;
            if (_shoppingCartSettings.RoundPrices)
                resultTemp = RoundingHelper.RoundPrice(resultTemp, _workContext.WorkingCurrency);

            if (!shoppingCartShipping.HasValue)
            {
                //we have errors
                return (null, discountAmount, appliedDiscounts, appliedGiftVouchers, redeemedLoyaltyPoints, redeemedLoyaltyPointsAmount);
            }

            decimal orderTotal = resultTemp;

            #region Loyalty points

            if (_loyaltyPointsSettings.Enabled)
            {
                if (!useLoyaltyPoints.HasValue)
                    useLoyaltyPoints = customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout, _workContext.CurrentStore.Id);

                if (useLoyaltyPoints.Value)
                {
                    int loyaltyPointsBalance = await _loyaltyPointsService.GetLoyaltyPointsBalance(customer.Id, _workContext.CurrentStore.Id);
                    if (CheckMinimumLoyaltyPointsToUseRequirement(loyaltyPointsBalance))
                    {
                        decimal loyaltyPointsBalanceAmount = await ConvertLoyaltyPointsToAmount(loyaltyPointsBalance);
                        if (orderTotal > decimal.Zero)
                        {
                            if (orderTotal > loyaltyPointsBalanceAmount)
                            {
                                redeemedLoyaltyPoints = loyaltyPointsBalance;
                                redeemedLoyaltyPointsAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(loyaltyPointsBalanceAmount, _workContext.WorkingCurrency);
                            }
                            else
                            {
                                redeemedLoyaltyPointsAmount = orderTotal;
                                redeemedLoyaltyPoints = ConvertAmountToLoyaltyPoints(await _currencyService.ConvertToPrimaryStoreCurrency(redeemedLoyaltyPointsAmount, _workContext.WorkingCurrency));
                            }
                        }
                    }
                }
            }

            #endregion

            orderTotal -= redeemedLoyaltyPointsAmount;
            if (_shoppingCartSettings.RoundPrices)
                orderTotal = RoundingHelper.RoundPrice(orderTotal, _workContext.WorkingCurrency);

            return (orderTotal, discountAmount, appliedDiscounts, appliedGiftVouchers, redeemedLoyaltyPoints, redeemedLoyaltyPointsAmount);
        }

        /// <summary>
        /// Converts existing loyalty points to amount
        /// </summary>
        /// <param name="loyaltyPoints">Loyalty points</param>
        /// <returns>Converted value</returns>
        public virtual async Task<decimal> ConvertLoyaltyPointsToAmount(int loyaltyPoints)
        {
            if (loyaltyPoints <= 0)
                return decimal.Zero;

            var result = loyaltyPoints * _loyaltyPointsSettings.ExchangeRate;
            if (_shoppingCartSettings.RoundPrices)
            {
                result = RoundingHelper.RoundPrice(result, _workContext.WorkingCurrency);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Converts an amount to loyalty points
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <returns>Converted value</returns>
        public virtual int ConvertAmountToLoyaltyPoints(decimal amount)
        {
            int result = 0;
            if (amount <= 0)
                return 0;

            if (_loyaltyPointsSettings.ExchangeRate > 0)
                result = (int)Math.Ceiling(amount / _loyaltyPointsSettings.ExchangeRate);
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether a customer has minimum amount of loyalty points to use (if enabled)
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
}
