using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders;

public class CountryReportModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Reports.Country.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Country.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }


    [GrandResourceDisplayName("Admin.Reports.Country.OrderStatus")]
    public int OrderStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Reports.Country.PaymentStatus")]
    public int PaymentStatusId { get; set; }

    public IList<SelectListItem> AvailableOrderStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentStatuses { get; set; } = new List<SelectListItem>();
}