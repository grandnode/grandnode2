using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Shipping;

public class WarehouseDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string AdminComment { get; set; }
}