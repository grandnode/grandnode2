using Grand.Domain;
using Grand.Domain.Discounts;
using MediatR;

namespace Grand.Business.Core.Queries.Catalog;

public record GetDiscountUsageHistoryQuery : IRequest<IPagedList<DiscountUsageHistory>>
{
    public string DiscountId { get; set; }
    public string CustomerId { get; set; }
    public string OrderId { get; set; }
    public bool? Canceled { get; set; } = null;
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = int.MaxValue;
}