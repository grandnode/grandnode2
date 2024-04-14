using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.MerchandiseReturn;

public class MerchandiseReturnListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchCustomerEmail")]
    public string SearchCustomerEmail { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchMerchandiseReturnStatus")]
    public int SearchMerchandiseReturnStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.GoDirectlyToId")]
    public int GoDirectlyToId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    public IList<SelectListItem> MerchandiseReturnStatus { get; set; } = new List<SelectListItem>();
}