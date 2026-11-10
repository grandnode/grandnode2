using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Shipping;

public class WarehouseLocationModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.WarehouseLocations.Fields.Warehouse")]
    public string WarehouseId { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.WarehouseLocations.Fields.Code")]
    public string Code { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.WarehouseLocations.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.WarehouseLocations.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     The warehouse's name, for the list
    /// </summary>
    public string Warehouse { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
}

public class WarehouseLocationListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.WarehouseLocations.Fields.Warehouse")]
    public string SearchWarehouseId { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
}
