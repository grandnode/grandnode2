using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;

namespace Grand.Business.Core.Interfaces.Catalog.Discounts;

/// <summary>
///     Discount service interface
/// </summary>
public interface IDiscountService
{
    /// <summary>
    ///     Gets a discount
    /// </summary>
    /// <param name="discountId">Discount id</param>
    /// <returns>Discount</returns>
    Task<Discount> GetDiscountById(string discountId);

    /// <summary>
    ///     Gets active discounts by context
    /// </summary>
    /// <returns>Discounts</returns>
    Task<IList<Discount>> GetActiveDiscountsByContext(DiscountType discountType, string storeId = "",
        string currencyCode = "");

    /// <summary>
    ///     Gets discounts query
    /// </summary>
    /// <returns>Discounts</returns>
    Task<IList<Discount>> GetDiscountsQuery(DiscountType? discountType, string storeId = "", string currencyCode = "",
        string couponCode = "", string discountName = "");

    /// <summary>
    ///     Inserts a new discount
    /// </summary>
    /// <param name="discount">Discount</param>
    Task InsertDiscount(Discount discount);

    /// <summary>
    ///     Updates the existing discount
    /// </summary>
    /// <param name="discount">Discount</param>
    Task UpdateDiscount(Discount discount);

    /// <summary>
    ///     Deletes the existing discount
    /// </summary>
    /// <param name="discount">Discount</param>
    Task DeleteDiscount(Discount discount);

    /// <summary>
    ///     Gets existing discount by coupon code
    /// </summary>
    /// <param name="couponCode">CouponCode</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Discount</returns>
    Task<Discount> GetDiscountByCouponCode(string couponCode, bool showHidden = false);

    /// <summary>
    ///     Gets a discount usage history item
    /// </summary>
    /// <param name="discountUsageHistoryId">Discount usage history item id</param>
    /// <returns>Discount usage history</returns>
    Task<DiscountUsageHistory> GetDiscountUsageHistoryById(string discountUsageHistoryId);

    /// <summary>
    ///     Inserts discount usage history item
    /// </summary>
    /// <param name="discountUsageHistory">Discount usage history item</param>
    Task InsertDiscountUsageHistory(DiscountUsageHistory discountUsageHistory);

    /// <summary>
    ///     Updates discount usage history item
    /// </summary>
    /// <param name="discountUsageHistory">Discount usage history item</param>
    Task UpdateDiscountUsageHistory(DiscountUsageHistory discountUsageHistory);

    /// <summary>
    ///     Deletes discount usage history item
    /// </summary>
    /// <param name="discountUsageHistory">Discount usage history item</param>
    Task DeleteDiscountUsageHistory(DiscountUsageHistory discountUsageHistory);

    /// <summary>
    ///     Gets all existing coupon codes for discount
    /// </summary>
    /// <param name="discountId"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    Task<IPagedList<DiscountCoupon>> GetAllCouponCodesByDiscountId(string discountId, int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets the discount code by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<DiscountCoupon> GetDiscountCodeById(string id);

    /// <summary>
    ///     Gets the discount code by code
    /// </summary>
    /// <param name="couponCode"></param>
    /// <returns></returns>
    Task<DiscountCoupon> GetDiscountCodeByCode(string couponCode);

    /// <summary>
    ///     Deletes discount code
    /// </summary>
    /// <param name="coupon"></param>
    Task DeleteDiscountCoupon(DiscountCoupon coupon);

    /// <summary>
    ///     Updates discount code - defines it as used or not
    /// </summary>
    /// <param name="couponCode"></param>
    /// <param name="used"></param>
    Task DiscountCouponSetAsUsed(string couponCode, bool used);

    /// <summary>
    ///     Cancels discount if order was canceled or deleted
    /// </summary>
    /// <param name="orderId"></param>
    Task CancelDiscount(string orderId);

    /// <summary>
    ///     Inserts discount code
    /// </summary>
    /// <param name="coupon"></param>
    Task InsertDiscountCoupon(DiscountCoupon coupon);

    /// <summary>
    ///     Get discount amount
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="amount">Amount</param>
    /// <param name="currency">currency</param>
    /// <param name="customer">Customer</param>
    /// <param name="product">Product</param>
    Task<double> GetDiscountAmount(Discount discount, Customer customer, Currency currency, Product product,
        double amount);

    /// <summary>
    ///     Gets preferred discount
    /// </summary>
    /// <param name="discounts"></param>
    /// <param name="customer"></param>
    /// <param name="product"></param>
    /// <param name="currency"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(
        IList<ApplyDiscount> discounts,
        Customer customer, Currency currency, Product product, double amount);

    /// <summary>
    ///     Get preferred discount
    /// </summary>
    /// <param name="discounts"></param>
    /// <param name="amount"></param>
    /// <param name="customer"></param>
    /// <param name="currency"></param>
    /// <returns>appliedDiscount and discountAmount</returns>
    Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(
        IList<ApplyDiscount> discounts,
        Customer customer, Currency currency, double amount);
}