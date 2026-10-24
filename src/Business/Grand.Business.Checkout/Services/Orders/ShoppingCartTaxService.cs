using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Tax;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Shopping cart tax calculation
/// </summary>
public class ShoppingCartTaxService : IShoppingCartTaxService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="shoppingCartSubTotalService">Shopping cart subtotal service</param>
    /// <param name="shoppingCartShippingService">Shopping cart shipping service</param>
    /// <param name="paymentService">Payment service</param>
    /// <param name="taxService">Tax service</param>
    /// <param name="taxSettings">Tax settings</param>
    /// <param name="shoppingCartSettings">Shopping cart settings</param>
    public ShoppingCartTaxService(IShoppingCartSubTotalService shoppingCartSubTotalService,
        IShoppingCartShippingService shoppingCartShippingService,
        IPaymentService paymentService,
        ITaxService taxService,
        TaxSettings taxSettings,
        ShoppingCartSettings shoppingCartSettings)
    {
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _shoppingCartShippingService = shoppingCartShippingService;
        _paymentService = paymentService;
        _taxService = taxService;
        _taxSettings = taxSettings;
        _shoppingCartSettings = shoppingCartSettings;
    }

    #endregion

    #region Fields

    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;
    private readonly IShoppingCartShippingService _shoppingCartShippingService;
    private readonly IPaymentService _paymentService;
    private readonly ITaxService _taxService;
    private readonly TaxSettings _taxSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion

    #region Methods

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
    public virtual async Task<ShoppingCartTaxTotal> GetTaxTotal(
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

        var shoppingCartSubTotal = await _shoppingCartSubTotalService.GetShoppingCartSubTotal(cart, context, false);
        var orderSubTotalTaxRates = shoppingCartSubTotal.TaxRates;

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
            var shippingTotalExcl = await _shoppingCartShippingService.GetShoppingCartShippingTotal(cart, context, false);
            var shippingExclTax = shippingTotalExcl.ShippingTotal;

            var shippingTotalIncl = await _shoppingCartShippingService.GetShoppingCartShippingTotal(cart, context, true);
            var shippingInclTax = shippingTotalIncl.ShippingTotal;
            var taxRate = shippingTotalIncl.TaxRate;

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

        return new ShoppingCartTaxTotal(taxTotal, taxRates);
    }

    #endregion
}
