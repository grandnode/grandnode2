﻿using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Catalog;

public class ProductAttributeCombinationDto : BaseApiEntityModel
{
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public bool AllowOutOfStockOrders { get; set; }
    public string Text { get; set; }
    public string Sku { get; set; }
    public string Mpn { get; set; }
    public string Gtin { get; set; }
    public double? OverriddenPrice { get; set; }
    public int NotifyAdminForQuantityBelow { get; set; }
    public string PictureId { get; set; }
}