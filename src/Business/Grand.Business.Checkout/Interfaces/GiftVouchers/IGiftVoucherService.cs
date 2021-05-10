using Grand.Domain;
using Grand.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.GiftVouchers
{
    /// <summary>
    /// Gift voucher service interface
    /// </summary>
    public partial interface IGiftVoucherService
    {
       
        /// <summary>
        /// Gets a gift voucher
        /// </summary>
        /// <param name="giftVoucherId">Gift voucher identifier</param>
        /// <returns>Gift voucher entry</returns>
        Task<GiftVoucher> GetGiftVoucherById(string giftVoucherId);

        /// <summary>
        /// Gets all gift vouchers
        /// </summary>
        /// <param name="purchasedWithOrderItemId">Associated order ID; null to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="isGiftVoucherActivated">Value indicating whether gift voucher is activated; null to load all records</param>
        /// <param name="giftVoucherCouponCode">Gift voucher coupon code; null to load all records</param>
        /// <param name="recipientName">Recipient name; null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Gift vouchers</returns>
        Task<IPagedList<GiftVoucher>> GetAllGiftVouchers(string purchasedWithOrderItemId = "",
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            bool? isGiftVoucherActivated = null, string giftVoucherCouponCode = null,
            string recipientName = null,
            int pageIndex = 0, int pageSize = int.MaxValue);


        /// <summary>
        /// Gets all gift vouchers usage history for orderId
        /// </summary>
        Task<IList<GiftVoucherUsageHistory>> GetAllGiftVoucherUsageHistory(string orderId = "");

        /// <summary>
        /// Inserts a gift voucher
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        Task InsertGiftVoucher(GiftVoucher giftVoucher);

        /// <summary>
        /// Updates the gift voucher
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        Task UpdateGiftVoucher(GiftVoucher giftVoucher);

        /// <summary>
        /// Deletes a gift voucher
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        Task DeleteGiftVoucher(GiftVoucher giftVoucher);

        /// <summary>
        /// Gets gift vouchers by 'PurchasedWithOrderItemId'
        /// </summary>
        /// <param name="purchasedWithOrderItemId">Purchased with order item identifier</param>
        /// <returns>Gift voucher entries</returns>
        Task<IList<GiftVoucher>> GetGiftVouchersByPurchasedWithOrderItemId(string purchasedWithOrderItemId);

        /// <summary>
        /// Generate new gift voucher code
        /// </summary>
        /// <returns>Result</returns>
        string GenerateGiftVoucherCode();
    }
}
