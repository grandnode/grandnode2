namespace Grand.Web.Models.ShoppingCart;

public record AddBidModel
{
    public string ProductId { get; set; }
    public string HighestBidValue { get; set; }
    public string WarehouseId { get; set; }
}