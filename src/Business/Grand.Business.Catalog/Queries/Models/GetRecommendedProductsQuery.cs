using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Catalog.Queries.Models
{
    public class GetRecommendedProductsQuery : IRequest<IList<Product>>
    {
        public string[] CustomerGroupIds { get; set; }
        public string StoreId { get; set; }
    }
}
