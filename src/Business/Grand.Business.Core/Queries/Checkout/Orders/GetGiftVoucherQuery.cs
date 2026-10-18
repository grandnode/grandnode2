using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetGiftVoucherQuery : IRequest<IPagedList<GiftVoucher>>
{
    public string GiftVoucherId { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string PurchasedWithOrderItemId { get; set; } = "";
    public bool? IsGiftVoucherActivated { get; set; }
    public string Code { get; set; } = "";
    public DateTime? CreatedFromUtc { get; set; } = null;
    public DateTime? CreatedToUtc { get; set; } = null;
    public string RecipientName { get; set; } = null;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = int.MaxValue;
}
