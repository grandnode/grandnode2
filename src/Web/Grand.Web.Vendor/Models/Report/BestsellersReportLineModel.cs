using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class BestsellersReportLineModel : BaseModel
{
    public string ProductId { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.Fields.Name")]
    public string ProductName { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.Fields.TotalAmount")]
    public string TotalAmount { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.Fields.TotalQuantity")]
    public double TotalQuantity { get; set; }
}