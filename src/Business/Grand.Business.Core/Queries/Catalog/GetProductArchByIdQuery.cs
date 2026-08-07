using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

public class GetProductArchByIdQuery : IRequest<Product>
{
    public string Id { get; set; }
}