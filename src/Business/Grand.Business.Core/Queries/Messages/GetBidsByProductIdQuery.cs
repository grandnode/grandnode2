using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Messages;

public class GetBidsByProductIdQuery : IRequest<IList<Bid>>
{
    public required string ProductId { get; set; }
}