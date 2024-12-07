using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Catalog;

public class ProductWarehouseInventoryDto : BaseApiEntityModel
{
    public string WarehouseId { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
}