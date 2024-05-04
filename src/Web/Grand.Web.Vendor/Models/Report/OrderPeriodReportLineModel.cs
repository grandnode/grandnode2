using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class OrderPeriodReportLineModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.Period.Name")]
    public string Period { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Period.Count")]
    public int Count { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Period.Amount")]
    public double Amount { get; set; }
}