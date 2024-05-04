using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class OrderIncompleteReportLineModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.Incomplete.Item")]
    public string Item { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Incomplete.Total")]
    public string Total { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Incomplete.Count")]
    public int Count { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Incomplete.View")]
    public string ViewLink { get; set; }
}