namespace Grand.Web.Models.ShoppingCart;

public class ProductCatalogCart
{
    public string ProductId { get; set; }
    public int ShoppingCartTypeId { get; set; }
    public int Quantity { get; set; } 
    public bool ForceRedirection { get; set; }
}