﻿using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Shipping;

public class PickupPointDto : BaseApiEntityModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string AdminComment { get; set; }
    public string WarehouseId { get; set; }
    public string StoreId { get; set; }
    public double PickupFee { get; set; }
    public int DisplayOrder { get; set; }
}