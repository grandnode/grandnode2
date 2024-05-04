using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Report;

public class CountryReportModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.Country.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Country.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Country.PaymentStatus")]
    public int PaymentStatusId { get; set; }

    public IList<SelectListItem> AvailablePaymentStatuses { get; set; } = new List<SelectListItem>();
}