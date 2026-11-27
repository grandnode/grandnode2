using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Common.Models;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.ShoppingCart;

public class SaveCheckoutAttributesCommand : IRequest<IList<CustomAttribute>>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }

    public required IList<ShoppingCartItem> Cart { get; set; }
    public required IList<CustomAttributeModel> SelectedAttributes { get; set; }
}