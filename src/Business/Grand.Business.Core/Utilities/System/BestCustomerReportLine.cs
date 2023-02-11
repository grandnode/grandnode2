namespace Grand.Business.Core.Utilities.System
{

    /// <summary>
    /// Represents a best customer report line
    /// </summary>
    public class BestCustomerReportLine
    {
        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the order total
        /// </summary>
        public double OrderTotal { get; set; }

        /// <summary>
        /// Gets or sets the order count
        /// </summary>
        public int OrderCount { get; set; }
    }
}
