using Grand.Business.Core.Utilities.Catalog;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Shopping cart shipping total
/// </summary>
/// <param name="ShippingTotal">Shipping total; null if it couldn't be calculated now</param>
/// <param name="TaxRate">Tax rate applied to shipping</param>
/// <param name="AppliedDiscounts">Shipping discounts applied</param>
public sealed record ShoppingCartShippingTotal(
    double? ShippingTotal,
    double TaxRate,
    List<ApplyDiscount> AppliedDiscounts);
