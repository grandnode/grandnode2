using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Messages;

public class GetBidsByProductIdQuery : IRequest<IList<Bid>>
{
    public string ProductId { get; set; }
}