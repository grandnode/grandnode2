using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Common.Models;
using Grand.Mediator;

namespace Grand.Web.Features.Models.ShoppingCart;

public class GetParseProductAttributes : IRequest<IList<CustomAttribute>>
{
    public Product Product { get; set; }
    public IList<CustomAttributeModel> Attributes { get; set; }
}