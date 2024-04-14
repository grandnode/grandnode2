using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Models.Shipping;

public class WarehouseModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.Warehouses.Fields.Code")]
    public string Code { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Warehouses.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Warehouses.Fields.AdminComment")]
    public string AdminComment { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Warehouses.Fields.Latitude")]
    public double? Latitude { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Warehouses.Fields.Longitude")]
    public double? Longitude { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Warehouses.Fields.Address")]
    public AddressModel Address { get; set; } = new();

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Warehouses.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }
}