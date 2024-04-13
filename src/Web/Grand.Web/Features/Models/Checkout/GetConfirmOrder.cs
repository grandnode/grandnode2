using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Models.Checkout;

public class GetConfirmOrder : IRequest<CheckoutConfirmModel>
{
    public Customer Customer { get; set; }
    public Language Language { get; set; }
    public Store Store { get; set; }
    public IList<ShoppingCartItem> Cart { get; set; }
}