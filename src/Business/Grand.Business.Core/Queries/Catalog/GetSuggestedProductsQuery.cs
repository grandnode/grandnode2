using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

public class GetSuggestedProductsQuery : IRequest<IList<Product>>
{
    public string[] CustomerTagIds { get; set; }
    public int ProductsNumber { get; set; }
    public Customer Customer { get; set; }
    public string StoreId { get; set; }
}