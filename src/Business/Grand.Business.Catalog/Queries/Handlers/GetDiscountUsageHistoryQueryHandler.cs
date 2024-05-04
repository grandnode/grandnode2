using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Discounts;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers;

public class
    GetDiscountUsageHistoryQueryHandler : IRequestHandler<GetDiscountUsageHistoryQuery,
    IPagedList<DiscountUsageHistory>>
{
    private readonly IRepository<DiscountUsageHistory> _discountUsageHistoryRepository;

    public GetDiscountUsageHistoryQueryHandler(IRepository<DiscountUsageHistory> discountUsageHistoryRepository)
    {
        _discountUsageHistoryRepository = discountUsageHistoryRepository;
    }

    public async Task<IPagedList<DiscountUsageHistory>> Handle(GetDiscountUsageHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = from d in _discountUsageHistoryRepository.Table
            select d;

        if (!string.IsNullOrEmpty(request.DiscountId))
            query = query.Where(duh => duh.DiscountId == request.DiscountId);
        if (!string.IsNullOrEmpty(request.CustomerId))
            query = query.Where(duh => duh.CustomerId == request.CustomerId);
        if (!string.IsNullOrEmpty(request.OrderId))
            query = query.Where(duh => duh.OrderId == request.OrderId);
        if (request.Canceled.HasValue)
            query = query.Where(duh => duh.Canceled == request.Canceled.Value);
        query = query.OrderByDescending(c => c.CreatedOnUtc);

        return await PagedList<DiscountUsageHistory>.Create(query, request.PageIndex, request.PageSize);
    }
}