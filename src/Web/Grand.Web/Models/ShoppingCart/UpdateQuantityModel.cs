using Grand.Domain.Orders;

namespace Grand.Web.Models.ShoppingCart;

public class UpdateQuantityModel
{
    public string ShoppingCartId { get; set; } 
    public int Quantity { get; set; }
    public ShoppingCartType ShoppingCartType { get; set; }

}