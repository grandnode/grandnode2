using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Messages.Queries.Models
{
    public class GetProductByIdQuery : IRequest<Product>
    {
        public string Id { get; set; }
    }
}
