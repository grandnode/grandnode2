using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class CountryReportLineModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.Country.Fields.CountryName")]
    public string CountryName { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Country.Fields.TotalOrders")]
    public int TotalOrders { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Country.Fields.SumOrders")]
    public string SumOrders { get; set; }
}