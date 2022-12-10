namespace Grand.Business.Core.Utilities.System
{
    /// <summary>
    /// Represents an "order by country" report line
    /// </summary>
    public class OrderByCountryReportLine
    {
        /// <summary>
        /// Country identifier; null for un know country
        /// </summary>
        public string CountryId { get; set; }

        /// <summary>
        /// Gets or sets the number of orders
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Gets or sets the order total summary
        /// </summary>
        public double SumOrders { get; set; }
    }
}
