using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Web.Models.Checkout;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Checkout;

public class GetPaymentMethod : IRequest<CheckoutPaymentMethodModel>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Currency Currency { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
    public required Language Language { get; set; }
    public required IList<ShoppingCartItem> Cart { get; set; }
    public string? FilterByCountryId { get; set; }
}