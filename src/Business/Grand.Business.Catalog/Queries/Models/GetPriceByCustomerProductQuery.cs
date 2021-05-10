using MediatR;

namespace Grand.Business.Catalog.Queries.Models
{
    public class GetPriceByCustomerProductQuery : IRequest<decimal?>
    {
        public string CustomerId { get; set; }
        public string ProductId { get; set; }
    }
}
