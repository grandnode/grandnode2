using Grand.Api.Models;

namespace Grand.Api.DTOs.Shipping;

public class WarehouseDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string AdminComment { get; set; }
}