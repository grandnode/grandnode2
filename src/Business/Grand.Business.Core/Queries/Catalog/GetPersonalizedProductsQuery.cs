using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

public class GetPersonalizedProductsQuery : IRequest<IList<Product>>
{
    public int ProductsNumber { get; set; }
    public required Customer Customer { get; set; }
    public required string StoreId { get; set; }
}