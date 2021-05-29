namespace Grand.Business.System.Utilities
{
    /// <summary>
    /// Represents an order average report line
    /// </summary>
    public partial class OrderAverageReportLine
    {
        /// <summary>
        /// Gets or sets the count
        /// </summary>
        public int CountOrders { get; set; }

        /// <summary>
        /// Gets or sets the shipping summary (excluding tax)
        /// </summary>
        public double SumShippingExclTax { get; set; }

        /// <summary>
        /// Gets or sets the tax summary
        /// </summary>
        public double SumTax { get; set; }

        /// <summary>
        /// Gets or sets the order total summary
        /// </summary>
        public double SumOrders { get; set; }
    }
}
