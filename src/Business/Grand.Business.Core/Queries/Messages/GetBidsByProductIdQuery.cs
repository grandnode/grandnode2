using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Queries.Messages
{
    public class GetBidsByProductIdQuery : IRequest<IList<Bid>>
    {
        public string ProductId { get; set; }
    }
}
