using Grand.Business.Core.Utilities.Catalog;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Shopping cart total with the order total discount, gift vouchers and loyalty points applied
/// </summary>
/// <param name="Total">Shopping cart total; null if it couldn't be calculated now</param>
/// <param name="DiscountAmount">Order total discount amount</param>
/// <param name="AppliedDiscounts">Discounts applied to the order total</param>
/// <param name="AppliedGiftVouchers">Gift vouchers applied</param>
/// <param name="RedeemedLoyaltyPoints">Loyalty points redeemed</param>
/// <param name="RedeemedLoyaltyPointsAmount">Amount the redeemed loyalty points cover</param>
public sealed record ShoppingCartTotal(
    double? Total,
    double DiscountAmount,
    List<ApplyDiscount> AppliedDiscounts,
    List<AppliedGiftVoucher> AppliedGiftVouchers,
    int RedeemedLoyaltyPoints,
    double RedeemedLoyaltyPointsAmount);
