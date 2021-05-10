namespace Grand.Business.Catalog.Utilities
{
    /// <summary>
    /// Represents a result of discount validation
    /// </summary>
    public partial class DiscountValidationResult
    {
        /// <summary>
        /// Gets or sets a value that indicates if discount is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets an error for a customer that should be visible when a coupon code is entered (in case if "IsValid" is set to "false")
        /// </summary>
        public string UserError { get; set; }

        /// <summary>
        /// Gets or sets a coupon code value
        /// </summary>
        public string CouponCode { get; set; }
    }
}
