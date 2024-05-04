using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Customers;

public class BestCustomersReportModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.OrderStatus")]
    public int OrderStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.PaymentStatus")]
    public int PaymentStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.ShippingStatus")]
    public int ShippingStatusId { get; set; }

    public string StoreId { get; set; }

    public IList<SelectListItem> AvailableOrderStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableShippingStatuses { get; set; } = new List<SelectListItem>();
}