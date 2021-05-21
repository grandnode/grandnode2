using Grand.Domain.Orders;
using MediatR;
using System;
using System.Linq;

namespace Grand.Business.Checkout.Queries.Models.Orders
{
    public class GetGiftVoucherQuery : IRequest<IQueryable<GiftVoucher>>
    {
        public string GiftVoucherId { get; set; } = "";
        public string PurchasedWithOrderItemId { get; set; } = "";
        public bool? IsGiftVoucherActivated { get; set; }
        public string Code { get; set; } = "";
        public DateTime? CreatedFromUtc { get; set; } = null;
        public DateTime? CreatedToUtc { get; set; } = null;
        public string RecipientName { get; set; } = null;
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = int.MaxValue;
    }
}
