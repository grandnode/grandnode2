using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class GetGiftVoucherQueryHandler : IRequestHandler<GetGiftVoucherQuery, IPagedList<GiftVoucher>>
{
    private readonly IRepository<GiftVoucher> _giftVoucherRepository;

    public GetGiftVoucherQueryHandler(IRepository<GiftVoucher> giftVoucherRepository)
    {
        _giftVoucherRepository = giftVoucherRepository;
    }

    public async Task<IPagedList<GiftVoucher>> Handle(GetGiftVoucherQuery request, CancellationToken cancellationToken)
    {
        var query = from p in _giftVoucherRepository.Table
            select p;

        if (!string.IsNullOrEmpty(request.GiftVoucherId))
            query = query.Where(gc => gc.Id == request.GiftVoucherId);

        if (!string.IsNullOrEmpty(request.StoreId))
            query = query.Where(gc => gc.StoreId == request.StoreId || gc.StoreId == null || gc.StoreId == "");

        if (!string.IsNullOrEmpty(request.PurchasedWithOrderItemId))
            query = query.Where(gc =>
                gc.PurchasedWithOrderItem != null && gc.PurchasedWithOrderItem.Id == request.PurchasedWithOrderItemId);

        if (request.CreatedFromUtc.HasValue)
            query = query.Where(gc => request.CreatedFromUtc.Value <= gc.CreatedOnUtc);
        if (request.CreatedToUtc.HasValue)
            query = query.Where(gc => request.CreatedToUtc.Value >= gc.CreatedOnUtc);
        if (request.IsGiftVoucherActivated.HasValue)
            query = query.Where(gc => gc.IsGiftVoucherActivated == request.IsGiftVoucherActivated.Value);
        if (!string.IsNullOrEmpty(request.Code))
            query = query.Where(gc => gc.Code != null && gc.Code == request.Code.ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(request.RecipientName))
            query = query.Where(c => c.RecipientName != null && c.RecipientName.Contains(request.RecipientName));

        query = query.OrderByDescending(gc => gc.CreatedOnUtc);

        return await _giftVoucherRepository.PagedAsync(query, request.PageIndex, request.PageSize, cancellationToken);
    }
}
