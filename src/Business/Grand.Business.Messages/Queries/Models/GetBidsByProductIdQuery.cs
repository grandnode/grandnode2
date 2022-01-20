using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Messages.Queries.Models
{
    public class GetBidsByProductIdQuery : IRequest<IList<Bid>>
    {
        public string ProductId { get; set; }
    }
}
