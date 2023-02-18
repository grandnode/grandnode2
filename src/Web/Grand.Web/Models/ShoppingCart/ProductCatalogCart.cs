using Grand.Domain.Orders;
using Grand.SharedKernel.Attributes;

namespace Grand.Web.Models.ShoppingCart;

public class ProductCatalogCart
{
    public string ProductId { get; set; }
    public ShoppingCartType ShoppingCartTypeId { get; set; }
    public int Quantity { get; set; }
    [IgnoreApi]
    public bool ForceRedirection { get; set; }
}