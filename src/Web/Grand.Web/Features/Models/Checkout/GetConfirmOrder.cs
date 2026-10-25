using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Checkout;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Checkout;

public class GetConfirmOrder : IRequest<CheckoutConfirmModel>, ICartCalculationRequest
{
    public Customer Customer { get; set; }
    public Language Language { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Currency Currency { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
    public IList<ShoppingCartItem> Cart { get; set; }
}