using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

public class GetRecommendedProductsQuery : IRequest<IList<Product>>
{
    public required string StoreId { get; set; }
    public required Customer Customer { get; set; }
}