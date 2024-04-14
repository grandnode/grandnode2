using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Report;

public class BestsellersReportModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.Store")]
    public string StoreId { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.PaymentStatus")]
    public int PaymentStatusId { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Bestsellers.BillingCountry")]
    public string BillingCountryId { get; set; }

    public IList<SelectListItem> AvailablePaymentStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
}