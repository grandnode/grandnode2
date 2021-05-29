using MediatR;

namespace Grand.Business.Catalog.Queries.Models
{
    public class GetPriceByCustomerProductQuery : IRequest<double?>
    {
        public string CustomerId { get; set; }
        public string ProductId { get; set; }
    }
}
