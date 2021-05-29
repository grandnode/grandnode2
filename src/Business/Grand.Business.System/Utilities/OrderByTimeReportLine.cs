namespace Grand.Business.System.Utilities
{
    /// <summary>
    /// Represents an "order by country" report line
    /// </summary>
    public partial class OrderByTimeReportLine
    {
        public string Time { get; set; }

        public int TotalOrders { get; set; }

        public double SumOrders { get; set; }
    }
}
