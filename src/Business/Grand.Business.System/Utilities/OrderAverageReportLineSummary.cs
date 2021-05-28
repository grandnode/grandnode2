using Grand.Domain.Orders;

namespace Grand.Business.System.Utilities
{
    /// <summary>
    /// Represents an order average report line summary
    /// </summary>
    public partial class OrderAverageReportLineSummary
    {
        /// <summary>
        /// Gets or sets the order status
        /// </summary>
        public int OrderStatus { get; set; }

        /// <summary>
        /// Gets or sets the sum today total
        /// </summary>
        public double SumTodayOrders { get; set; }

        /// <summary>
        /// Gets or sets the today count
        /// </summary>
        public int CountTodayOrders { get; set; }

        /// <summary>
        /// Gets or sets the sum this week total
        /// </summary>
        public double SumThisWeekOrders { get; set; }

        /// <summary>
        /// Gets or sets the this week count
        /// </summary>
        public int CountThisWeekOrders { get; set; }

        /// <summary>
        /// Gets or sets the sum this month total
        /// </summary>
        public double SumThisMonthOrders { get; set; }

        /// <summary>
        /// Gets or sets the this month count
        /// </summary>
        public int CountThisMonthOrders { get; set; }

        /// <summary>
        /// Gets or sets the sum this year total
        /// </summary>
        public double SumThisYearOrders { get; set; }

        /// <summary>
        /// Gets or sets the this year count
        /// </summary>
        public int CountThisYearOrders { get; set; }

        /// <summary>
        /// Gets or sets the sum all time total
        /// </summary>
        public double SumAllTimeOrders { get; set; }

        /// <summary>
        /// Gets or sets the all time count
        /// </summary>
        public int CountAllTimeOrders { get; set; }
    }
}
