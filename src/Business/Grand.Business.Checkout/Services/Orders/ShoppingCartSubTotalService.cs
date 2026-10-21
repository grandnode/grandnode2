using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
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
///     Shopping cart subtotal calculation
/// </summary>
public class ShoppingCartSubTotalService : IShoppingCartSubTotalService
{
    #region Ctor

    public ShoppingCartSubTotalService(IPricingService pricingService,
        ITaxService taxService,
        ICheckoutAttributeParser checkoutAttributeParser,
        IDiscountService discountService,
        IDiscountValidationService discountValidationService,
        IProductService productService,
        ICurrencyService currencyService,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings)
    {
        _pricingService = pricingService;
        _taxService = taxService;
        _checkoutAttributeParser = checkoutAttributeParser;
        _discountService = discountService;
        _discountValidationService = discountValidationService;
        _productService = productService;
        _currencyService = currencyService;
        _shoppingCartSettings = shoppingCartSettings;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Fields

    private readonly IPricingService _pricingService;
    private readonly ITaxService _taxService;
    private readonly ICheckoutAttributeParser _checkoutAttributeParser;
    private readonly IDiscountService _discountService;
    private readonly IDiscountValidationService _discountValidationService;
    private readonly IProductService _productService;
    private readonly ICurrencyService _currencyService;
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
            store.Id, currency.CurrencyCode);
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
                        CouponCode = validDiscount.CouponCode,
                        MaximumDiscountedQuantity = discount.MaximumDiscountedQuantity
                    });
            }

        var preferredDiscounts = await _discountService.GetPreferredDiscount(allowedDiscounts, customer,
            currency, orderSubTotal);
        appliedDiscounts = preferredDiscounts.appliedDiscount;
        discountAmount = preferredDiscounts.discountAmount;

        if (discountAmount < 0)
            discountAmount = 0;

        return (discountAmount, appliedDiscounts);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Gets shopping cart subtotal
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    public virtual async Task<ShoppingCartSubTotal> GetShoppingCartSubTotal(IList<ShoppingCartItem> cart,
        CalculationContext context, bool includingTax)
    {
        double discountAmount = 0;
        var appliedDiscounts = new List<ApplyDiscount>();
        double subTotalWithoutDiscount = 0;
        double subTotalWithDiscount = 0;
        var taxRates = new SortedDictionary<double, double>();

        if (!cart.Any())
            return new ShoppingCartSubTotal(discountAmount, appliedDiscounts, subTotalWithoutDiscount,
                subTotalWithDiscount, taxRates);

        //get the customer 
        var customer = context.Customer;
        var store = context.Store;

        //sub totals
        double subTotalExclTaxWithoutDiscount = 0;
        double subTotalInclTaxWithoutDiscount = 0;
        foreach (var shoppingCartItem in cart)
        {
            var product = await _productService.GetProductById(shoppingCartItem.ProductId);
            if (product == null)
                continue;

            var subtotal = await _pricingService.GetSubTotal(shoppingCartItem, product,
                customer, store, context.Currency);
            var sciSubTotal = subtotal.subTotal;

            var pricesExcl = await _taxService.GetProductPrice(product, sciSubTotal, false, customer, store);
            var sciExclTax = pricesExcl.productprice;

            var (sciInclTax, taxRate) = await _taxService.GetProductPrice(product, sciSubTotal, true, customer, store);

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
            var checkoutAttributes = customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, store.Id);
            var attributeValues = await _checkoutAttributeParser.ParseCheckoutAttributeValue(checkoutAttributes);
            foreach (var attributeValue in attributeValues)
            {
                var checkoutAttributePriceExclTax = await _taxService.GetCheckoutAttributePrice(attributeValue.ca, attributeValue.cav, false, customer, store);
                
                var caExclTax = await _currencyService.ConvertFromPrimaryStoreCurrency(checkoutAttributePriceExclTax.checkoutPrice,
                    context.Currency);
                var (checkoutPrice, taxRate) = await _taxService.GetCheckoutAttributePrice(attributeValue.ca, attributeValue.cav, true, customer, store);
                var caInclTax = await _currencyService.ConvertFromPrimaryStoreCurrency(checkoutPrice,
                    context.Currency);

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
            subTotalWithoutDiscount = RoundingHelper.RoundPrice(subTotalWithoutDiscount,
                context.Currency);

        //We calculate discount amount on order subtotal excl tax (discount first)
        //calculate discount amount ('Applied to order subtotal' discount)
        var orderSubtotalDiscount = await GetOrderSubtotalDiscount(customer, store, context.Currency,
            subTotalExclTaxWithoutDiscount);
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
                    taxValue = RoundingHelper.RoundPrice(taxValue, context.Currency);
                taxRates[taxRate] = taxValue;
            }

            //subtotal with discount (incl tax)
            subTotalInclTaxWithDiscount += taxValue;
        }

        if (_shoppingCartSettings.RoundPrices)
        {
            discountAmountInclTax = RoundingHelper.RoundPrice(discountAmountInclTax,
                context.Currency);
            discountAmountExclTax = RoundingHelper.RoundPrice(discountAmountExclTax,
                context.Currency);
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
            subTotalWithDiscount = RoundingHelper.RoundPrice(subTotalWithDiscount,
                context.Currency);

        return new ShoppingCartSubTotal(discountAmount, appliedDiscounts, subTotalWithoutDiscount,
            subTotalWithDiscount, taxRates);
    }

    #endregion
}
