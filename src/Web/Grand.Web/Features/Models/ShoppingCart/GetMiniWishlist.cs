using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Models.ShoppingCart;
using MediatR;

namespace Grand.Web.Features.Models.ShoppingCart
{
    public class GetMiniWishlist : IRequest<MiniWishlistModel>
    {
        public Customer Customer { get; set; }
        public Language Language { get; set; }
        public Currency Currency { get; set; }
        public Store Store { get; set; }
        public IList<ShoppingCartItem> Cart { get; set; }
    }
}
