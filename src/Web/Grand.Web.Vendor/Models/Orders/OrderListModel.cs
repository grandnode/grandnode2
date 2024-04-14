using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Orders;

public class OrderListModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Orders.List.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.BillingEmail")]
    public string BillingEmail { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.BillingLastName")]
    public string BillingLastName { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.OrderStatus")]
    public int OrderStatusId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.PaymentStatus")]
    public int PaymentStatusId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.ShippingStatus")]
    public int ShippingStatusId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.PaymentMethod")]
    public string PaymentMethodSystemName { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.Warehouse")]
    public string WarehouseId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.Product")]
    public string ProductId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.BillingCountry")]
    public string BillingCountryId { get; set; }


    [GrandResourceDisplayName("Vendor.Orders.List.OrderGuid")]

    public string OrderGuid { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.GoDirectlyToNumber")]

    public string GoDirectlyToNumber { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.List.OrderTagId")]
    public string OrderTag { get; set; }

    public IList<SelectListItem> AvailableOrderStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableShippingStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentMethods { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableOrderTags { get; set; } = new List<SelectListItem>();
}