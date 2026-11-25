using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

public class GetPriceByCustomerProductQuery : IRequest<double?>
{
    public required string CustomerId { get; set; }
    public required string ProductId { get; set; }
}