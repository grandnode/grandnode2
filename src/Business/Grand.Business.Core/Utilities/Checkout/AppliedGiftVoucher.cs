using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout
{
    /// <summary>
    /// Applied gift voucher
    /// </summary>
    public class AppliedGiftVoucher
    {
        /// <summary>
        /// Gets or sets the used value
        /// </summary>
        public double AmountCanBeUsed { get; set; }

        /// <summary>
        /// Gets the gift voucher
        /// </summary>
        public GiftVoucher GiftVoucher { get; set; }
    }
}
