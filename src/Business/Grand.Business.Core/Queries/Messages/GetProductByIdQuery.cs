using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Queries.Messages
{
    public class GetProductByIdQuery : IRequest<Product>
    {
        public string Id { get; set; }
    }
}
