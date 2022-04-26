using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Queries.Catalog
{
    public class GetSuggestedProductsQuery : IRequest<IList<Product>>
    {
        public string[] CustomerTagIds { get; set; }
        public int ProductsNumber { get; set; }
    }
}
