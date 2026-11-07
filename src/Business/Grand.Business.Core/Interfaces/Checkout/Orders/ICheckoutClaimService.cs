using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Takes what an order spends - stock, limited discounts and single-use coupons, gift voucher balances, loyalty
///     points - before the payment, and gives it back when no order comes of it
/// </summary>
public interface ICheckoutClaimService
{
    /// <summary>
    ///     Takes everything the order spends, or nothing: on the first refusal what was taken is given back
    /// </summary>
    /// <param name="details">Place order container; its OrderId identifies the claim</param>
    /// <param name="orderCode">Order code, for the loyalty points history</param>
    Task<CheckoutClaim> Claim(PlaceOrderContainer details, string orderCode);

    /// <summary>
    ///     Gives back everything the claim holds; a step that fails is logged and the others still run
    /// </summary>
    Task Release(CheckoutClaim claim);
}
