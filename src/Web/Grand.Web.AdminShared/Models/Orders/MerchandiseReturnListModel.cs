using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Orders;

public class MerchandiseReturnListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchCustomerEmail")]
    public string SearchCustomerEmail { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchMerchandiseReturnStatus")]
    public int SearchMerchandiseReturnStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.GoDirectlyToId")]
    public string GoDirectlyToId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchOrderNumber")]
    public int? SearchOrderNumber { get; set; }

    //the vendor panel's own vendor is taken from its scope; a vendor posted there is ignored
    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchVendorId")]
    public string SearchVendorId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchReturnShippingProvider")]
    public string SearchReturnShippingProvider { get; set; }

    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.SearchReasonId")]
    public string SearchReasonId { get; set; }

    //returns pending, accepted or received for more than this many days
    [GrandResourceDisplayName("Admin.Orders.MerchandiseReturns.List.OpenLongerThanDays")]
    public int? OpenLongerThanDays { get; set; }

    public string StoreId { get; set; }

    public IList<SelectListItem> MerchandiseReturnStatus { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> AvailableReturnShippingProviders { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> AvailableReasons { get; set; } = new List<SelectListItem>();
}