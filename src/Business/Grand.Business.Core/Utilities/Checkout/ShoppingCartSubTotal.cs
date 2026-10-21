using Grand.Business.Core.Utilities.Catalog;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Shopping cart subtotal: items and checkout attributes, before shipping, payment fee and order total discounts
/// </summary>
/// <param name="DiscountAmount">Subtotal discount amount</param>
/// <param name="AppliedDiscounts">Discounts applied to the subtotal</param>
/// <param name="SubTotalWithoutDiscount">Subtotal before the subtotal discount</param>
/// <param name="SubTotalWithDiscount">Subtotal after the subtotal discount</param>
/// <param name="TaxRates">Tax amount per tax rate</param>
public sealed record ShoppingCartSubTotal(
    double DiscountAmount,
    List<ApplyDiscount> AppliedDiscounts,
    double SubTotalWithoutDiscount,
    double SubTotalWithDiscount,
    SortedDictionary<double, double> TaxRates);
