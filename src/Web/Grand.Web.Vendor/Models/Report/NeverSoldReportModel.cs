using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Report;

public class NeverSoldReportModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.NeverSold.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.NeverSold.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }
}