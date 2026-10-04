using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Shipping;

public class PickupPointModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Description")]
    [NoHtml]
    public string Description { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.AdminComment")]
    [NoHtml]
    public string AdminComment { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Address")]
    public AddressModel Address { get; set; } = new();

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Warehouses")]
    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();

    public string WarehouseId { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Stores")]
    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public string StoreId { get; set; }

    public string StoreName { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.PickupFee")]
    public double PickupFee { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Latitude")]
    public double? Latitude { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Longitude")]
    public double? Longitude { get; set; }
}