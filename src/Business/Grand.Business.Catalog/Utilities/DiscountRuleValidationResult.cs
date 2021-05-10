namespace Grand.Business.Catalog.Utilities
{
    /// <summary>
    /// Represents a result of discount requirement validation
    /// </summary>
    public partial class DiscountRuleValidationResult
    {
        /// <summary>
        /// Gets or sets a value that indicates if discount is valid or not
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets an error for a customer that should be visible when a coupon code is entered (in case if "IsValid" is set to "false")
        /// </summary>
        public string UserError { get; set; }
    }
}
