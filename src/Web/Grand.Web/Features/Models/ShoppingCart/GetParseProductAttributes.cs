using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.ShoppingCart
{
    public class GetParseProductAttributes : IRequest<IList<CustomAttribute>>
    {
        public Product Product { get; set; }
        public ProductModel Model { get; set; }
    }
}
