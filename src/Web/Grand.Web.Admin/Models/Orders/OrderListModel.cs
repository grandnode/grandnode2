using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders;

public class OrderListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Orders.List.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.BillingEmail")]
    public string BillingEmail { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.BillingLastName")]
    public string BillingLastName { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.OrderStatus")]
    public int OrderStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.PaymentStatus")]
    public int PaymentStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.ShippingStatus")]
    public int? ShippingStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.PaymentMethod")]
    public string PaymentMethodSystemName { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.Store")]
    public string StoreId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.Vendor")]
    public string VendorId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.Warehouse")]
    public string WarehouseId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.Product")]
    public string ProductId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.BillingCountry")]
    public string BillingCountryId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.OrderNotes")]

    public string OrderNotes { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.OrderGuid")]

    public string OrderGuid { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.GoDirectlyToNumber")]

    public string GoDirectlyToNumber { get; set; }

    [GrandResourceDisplayName("Admin.Orders.List.OrderTagId")]
    public string OrderTag { get; set; }

    public IList<SelectListItem> AvailableOrderStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableShippingStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentMethods { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableOrderTags { get; set; } = new List<SelectListItem>();
}