namespace Grand.Web.Models.ShoppingCart;

public class AddCartFromWishlistModel
{
    public Guid? CustomerGuid { get; set; }
    public string ShoppingCartId { get; set; }
}