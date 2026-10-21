using Grand.Business.Core.Utilities.Catalog;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Shipping rate after free shipping, additional charges and shipping discounts
/// </summary>
/// <param name="ShippingRate">Adjusted shipping rate</param>
/// <param name="AppliedDiscounts">Shipping discounts applied</param>
public sealed record ShippingRateAdjustment(double ShippingRate, List<ApplyDiscount> AppliedDiscounts);
