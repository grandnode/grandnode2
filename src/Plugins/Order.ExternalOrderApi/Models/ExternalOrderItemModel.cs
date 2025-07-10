using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Order.ExternalOrderApi.Models;

public class ExternalOrderItemModel
{
    [Required]
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }
    
    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }
    
    [Required]
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [Required]
    [JsonPropertyName("price")]
    public double Price { get; set; }
    
    [JsonPropertyName("amount")]
    public double Amount { get; set; }
    
    [JsonPropertyName("discount")]
    public double Discount { get; set; }
    
    [JsonPropertyName("tyDiscount")]
    public double TyDiscount { get; set; }
    
    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }
    
    [JsonPropertyName("id")]
    public long Id { get; set; } // orderLineId
    
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }
    
    [JsonPropertyName("productCode")]
    public long? ProductCode { get; set; }
    
    [JsonPropertyName("productSize")]
    public string? ProductSize { get; set; }
    
    [JsonPropertyName("productColor")]
    public string? ProductColor { get; set; }
    
    [JsonPropertyName("productOrigin")]
    public string? ProductOrigin { get; set; }
    
    [JsonPropertyName("merchantId")]
    public long? MerchantId { get; set; }
    
    [JsonPropertyName("merchantSku")]
    public string? MerchantSku { get; set; }
    
    [JsonPropertyName("salesCampaignId")]
    public long? SalesCampaignId { get; set; }
    
    [JsonPropertyName("orderLineItemStatusName")]
    public string? OrderLineItemStatusName { get; set; }
    
    [JsonPropertyName("vatBaseAmount")]
    public double? VatBaseAmount { get; set; }
    
    [JsonPropertyName("productCategoryId")]
    public long? ProductCategoryId { get; set; }
    
    [JsonPropertyName("laborCost")]
    public double? LaborCost { get; set; }

    [JsonPropertyName("discountDetails")]
    public List<DiscountDetailModel>? DiscountDetails { get; set; }
    
    [JsonPropertyName("fastDeliveryOptions")]
    public List<FastDeliveryOptionModel>? FastDeliveryOptions { get; set; }
}

/// <summary>
/// Model for discount details
/// </summary>
public class DiscountDetailModel
{
    [JsonPropertyName("lineItemPrice")]
    public double LineItemPrice { get; set; }
    
    [JsonPropertyName("lineItemDiscount")]
    public double LineItemDiscount { get; set; }
    
    [JsonPropertyName("lineItemTyDiscount")]
    public double LineItemTyDiscount { get; set; }
}

/// <summary>
/// Model for fast delivery options
/// </summary>
public class FastDeliveryOptionModel
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
