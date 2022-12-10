namespace Grand.Business.Core.Utilities.System
{
    /// <summary>
    /// Represents an "order by country" report line
    /// </summary>
    public class OrderByTimeReportLine
    {
        public string Time { get; set; }

        public int TotalOrders { get; set; }

        public double SumOrders { get; set; }
    }
}
