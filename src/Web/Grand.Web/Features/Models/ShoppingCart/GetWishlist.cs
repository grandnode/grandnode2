using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Web.Models.ShoppingCart;
using Grand.Mediator;

namespace Grand.Web.Features.Models.ShoppingCart;

public class GetWishlist : IRequest<WishlistModel>, ICartCalculationRequest
{
    /// <summary>
    ///     Owner of the wishlist
    /// </summary>
    public Customer Customer { get; set; }

    /// <summary>
    ///     Customer viewing the wishlist; prices are calculated for them, which differs from Customer on a shared wishlist
    /// </summary>
    public Customer CurrentCustomer { get; set; }
    public Language Language { get; set; }
    public Currency Currency { get; set; }
    public Domain.Stores.Store Store { get; set; }

    /// <summary>
    ///     Store address of the request, used for the download links of product attributes
    /// </summary>
    public DomainHost Host { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
    public bool IsEditable { get; set; } = true;
    public IList<ShoppingCartItem> Cart { get; set; }
}