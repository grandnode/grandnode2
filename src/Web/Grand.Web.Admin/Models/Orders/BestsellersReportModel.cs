using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders;

public class BestsellersReportModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Reports.Bestsellers.Store")]
    public string StoreId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Bestsellers.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Bestsellers.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }


    [GrandResourceDisplayName("Admin.Reports.Bestsellers.OrderStatus")]
    public int OrderStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Bestsellers.PaymentStatus")]
    public int PaymentStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Bestsellers.BillingCountry")]
    public string BillingCountryId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Bestsellers.Vendor")]
    public string VendorId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> AvailableOrderStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
}