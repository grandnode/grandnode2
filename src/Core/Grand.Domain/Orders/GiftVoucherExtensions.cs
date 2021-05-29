using Grand.Domain.Directory;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class GiftVoucherExtensions
    {
        /// <summary>
        /// Gets a gift voucher remaining amount
        /// </summary>
        /// <returns>Gift voucher remaining amount</returns>
        public static double GetGiftVoucherRemainingAmount(this GiftVoucher giftVoucher)
        {
            double result = giftVoucher.Amount;

            foreach (var gcuh in giftVoucher.GiftVoucherUsageHistory)
                result -= gcuh.UsedValue;

            if (result < 0)
                result = 0;

            return result;
        }

        /// <summary>
        /// Is gift voucher valid
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        /// <returns>Result</returns>
        public static bool IsGiftVoucherValid(this GiftVoucher giftVoucher, Currency currency)
        {
            if (!giftVoucher.IsGiftVoucherActivated)
                return false;

            if (giftVoucher.CurrencyCode != currency.CurrencyCode)
                return false;

            if(giftVoucher.ValidTo.HasValue && giftVoucher.ValidTo.Value <= System.DateTime.UtcNow)
                return false;

            double remainingAmount = giftVoucher.GetGiftVoucherRemainingAmount();
            if (remainingAmount > 0)
                return true;

            return false;
        }
    }
}
