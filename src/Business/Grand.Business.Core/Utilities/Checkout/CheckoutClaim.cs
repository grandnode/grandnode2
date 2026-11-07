using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Discounts;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     What an order attempt took of the stock, the limited discounts, the gift vouchers and the loyalty points, so it
///     can be given back as a whole
/// </summary>
public class CheckoutClaim
{
    /// <summary>
    ///     The identifier the order is inserted with
    /// </summary>
    public string OrderId { get; init; }

    public string OrderCode { get; init; }
    public string CustomerId { get; init; }
    public string StoreId { get; init; }

    /// <summary>
    ///     Everything the order spends was taken; when false, nothing is held
    /// </summary>
    public bool Succeeded { get; set; }

    public IList<ClaimedStock> Stock { get; } = new List<ClaimedStock>();
    public IList<DiscountUsageHistory> DiscountUsages { get; } = new List<DiscountUsageHistory>();
    public IList<string> GiftVoucherIds { get; } = new List<string>();
    public int LoyaltyPoints { get; set; }
}

/// <summary>
///     A quantity reserved for one cart line
/// </summary>
public record ClaimedStock(Product Product, int Quantity, IList<CustomAttribute> Attributes, string WarehouseId);
