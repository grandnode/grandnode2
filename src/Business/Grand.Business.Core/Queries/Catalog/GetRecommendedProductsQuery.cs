using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

public class GetRecommendedProductsQuery : IRequest<IList<Product>>
{
    public string[] CustomerGroupIds { get; set; }
    public string StoreId { get; set; }
}