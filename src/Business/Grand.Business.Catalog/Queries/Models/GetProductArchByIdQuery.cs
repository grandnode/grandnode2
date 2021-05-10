using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Catalog.Queries.Models
{
    public class GetProductArchByIdQuery : IRequest<Product>
    {
        public string Id { get; set; }
    }
}
