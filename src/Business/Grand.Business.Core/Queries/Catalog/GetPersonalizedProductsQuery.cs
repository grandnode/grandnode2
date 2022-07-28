using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Queries.Catalog
{
    public class GetPersonalizedProductsQuery : IRequest<IList<Product>>
    {
        public string CustomerId { get; set; }
        public int ProductsNumber { get; set; }
    }
}
