namespace Grand.Business.Core.Utilities.System
{
    /// <summary>
    /// Represents a best sellers report line
    /// </summary>
    [Serializable]
    public class BestsellersReportLine
    {
        /// <summary>
        /// Gets or sets the product identifier
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the total amount
        /// </summary>
        public double TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the total quantity
        /// </summary>
        public int TotalQuantity { get; set; }

    }
}
