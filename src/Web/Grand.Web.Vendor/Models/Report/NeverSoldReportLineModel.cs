using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class NeverSoldReportLineModel : BaseModel
{
    public string ProductId { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.NeverSold.Fields.Name")]
    public string ProductName { get; set; }
}