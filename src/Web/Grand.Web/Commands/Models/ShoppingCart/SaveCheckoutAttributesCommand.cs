using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Common.Models;
using MediatR;

namespace Grand.Web.Commands.Models.ShoppingCart;

public class SaveCheckoutAttributesCommand : IRequest<IList<CustomAttribute>>
{
    public Customer Customer { get; set; }
    public Store Store { get; set; }

    public IList<ShoppingCartItem> Cart { get; set; }
    public IList<CustomAttributeModel> SelectedAttributes { get; set; }
}