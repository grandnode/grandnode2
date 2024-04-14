using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Shipment;

public class ShipmentListModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Orders.Shipments.List.StartDate")]
    [UIHint("DateNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.List.EndDate")]
    [UIHint("DateNullable")]
    public DateTime? EndDate { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.List.TrackingNumber")]

    public string TrackingNumber { get; set; }


    [GrandResourceDisplayName("Vendor.Orders.Shipments.List.City")]

    public string City { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.List.LoadNotShipped")]
    public bool LoadNotShipped { get; set; }


    [GrandResourceDisplayName("Vendor.Orders.Shipments.List.Warehouse")]
    public string WarehouseId { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
}