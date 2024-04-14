using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class OrderAverageReportLineSummaryModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.Average.SumTodayOrders")]
    public string SumTodayOrders { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Average.SumThisWeekOrders")]
    public string SumThisWeekOrders { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Average.SumThisMonthOrders")]
    public string SumThisMonthOrders { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Average.SumThisYearOrders")]
    public string SumThisYearOrders { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Average.SumAllTimeOrders")]
    public string SumAllTimeOrders { get; set; }
}