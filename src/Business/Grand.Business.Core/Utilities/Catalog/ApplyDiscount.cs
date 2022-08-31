namespace Grand.Business.Core.Utilities.Catalog
{
    /// <summary>
    /// Applied discount
    /// </summary>
    public class ApplyDiscount
    {
        /// <summary>
        /// Gets or sets the discount Id
        /// </summary>
        public string DiscountId { get; set; }

        /// <summary>
        /// Gets or sets the discount 
        /// </summary>
        public string CouponCode { get; set; }


        /// <summary>
        /// Gets or sets is discount is cumulative
        /// </summary>
        public bool IsCumulative { get; set; }
    }
}
