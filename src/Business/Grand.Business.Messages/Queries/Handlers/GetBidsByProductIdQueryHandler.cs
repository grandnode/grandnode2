using Grand.Business.Core.Queries.Messages;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Messages.Queries.Handlers;

public class GetBidsByProductIdQueryHandler : IRequestHandler<GetBidsByProductIdQuery, IList<Bid>>
{
    private readonly IRepository<Bid> _bidRepository;

    public GetBidsByProductIdQueryHandler(IRepository<Bid> bidRepository)
    {
        _bidRepository = bidRepository;
    }

    public async Task<IList<Bid>> Handle(GetBidsByProductIdQuery request, CancellationToken cancellationToken)
    {
        return await _bidRepository.ToListAsync(_bidRepository
            .Table.Where(x => x.ProductId == request.ProductId)
            .OrderByDescending(x => x.Date), cancellationToken);
    }
}